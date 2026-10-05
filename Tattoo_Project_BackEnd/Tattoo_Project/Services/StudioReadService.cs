using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.StudioDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services;

/// <summary>
/// Builds the public studio read model without materializing the full EF entity graph.
/// Keep all public-studio visibility and DTO shaping in this service so Explore,
/// studio details and Favorites cannot drift apart.
/// </summary>
public sealed class StudioReadService(
    TattooDbContext context,
    IPrivateMediaUrlService mediaUrls,
    TimeProvider timeProvider,
    ILogger<StudioReadService> logger)
{
    public async Task<List<StudioDto>> GetPublicStudiosAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var studioRows = await VisibleStudios(now)
            .OrderBy(s => s.Name)
            .ThenBy(s => s.City)
            .Select(s => new StudioRow(
                s.Id, s.OwnerArtistId, s.Name, s.Description, s.Address, s.City,
                s.Country, s.Latitude, s.Longitude, s.IsOpenForJoinRequests,
                s.CoverImageUrl, s.LogoImageUrl))
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(search) && studioRows.Count > 0)
            studioRows = await FilterSearchAsync(studioRows, search, now, cancellationToken);

        return await BuildDtosAsync(studioRows, now, cancellationToken);
    }

    public async Task<StudioDto?> GetPublicStudioAsync(
        int studioId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var row = await VisibleStudios(now)
            .Where(s => s.Id == studioId)
            .Select(s => new StudioRow(
                s.Id, s.OwnerArtistId, s.Name, s.Description, s.Address, s.City,
                s.Country, s.Latitude, s.Longitude, s.IsOpenForJoinRequests,
                s.CoverImageUrl, s.LogoImageUrl))
            .SingleOrDefaultAsync(cancellationToken);

        if (row == null) return null;
        return (await BuildDtosAsync([row], now, cancellationToken)).Single();
    }

    public async Task<List<StudioDto>> GetPublicStudiosByIdAsync(
        IReadOnlyList<int> orderedStudioIds,
        CancellationToken cancellationToken = default)
    {
        if (orderedStudioIds.Count == 0) return [];

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var distinctIds = orderedStudioIds.Distinct().ToArray();
        var rows = await VisibleStudios(now)
            .Where(s => distinctIds.Contains(s.Id))
            .Select(s => new StudioRow(
                s.Id, s.OwnerArtistId, s.Name, s.Description, s.Address, s.City,
                s.Country, s.Latitude, s.Longitude, s.IsOpenForJoinRequests,
                s.CoverImageUrl, s.LogoImageUrl))
            .ToListAsync(cancellationToken);

        var dtos = await BuildDtosAsync(rows, now, cancellationToken);
        var byId = dtos.ToDictionary(s => s.Id);
        return orderedStudioIds
            .Distinct()
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToList();
    }

    private IQueryable<Studio> VisibleStudios(DateTime now) =>
        context.Studios
            .AsNoTracking()
            .Where(s => s.Artists.Any(a =>
                a.Subscription != null &&
                ((a.Subscription.Status == ArtistSubscriptionStatuses.Trialing && a.Subscription.TrialEndsAt > now) ||
                 (a.Subscription.Status == ArtistSubscriptionStatuses.GracePeriod && a.Subscription.CurrentPeriodEndsAt > now) ||
                 (a.Subscription.Status == ArtistSubscriptionStatuses.Active && a.Subscription.CurrentPeriodEndsAt > now))));

    private IQueryable<TattooArtist> VisibleArtists(DateTime now) =>
        context.TattooArtists
            .AsNoTracking()
            .Where(a => a.Subscription != null &&
                ((a.Subscription.Status == ArtistSubscriptionStatuses.Trialing && a.Subscription.TrialEndsAt > now) ||
                 (a.Subscription.Status == ArtistSubscriptionStatuses.GracePeriod && a.Subscription.CurrentPeriodEndsAt > now) ||
                 (a.Subscription.Status == ArtistSubscriptionStatuses.Active && a.Subscription.CurrentPeriodEndsAt > now)));

    private async Task<List<StudioRow>> FilterSearchAsync(
        List<StudioRow> studioRows,
        string search,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tokens = StudioService.NormalizeSearch(search)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return studioRows;

        var studioIds = studioRows.Select(s => s.Id).ToArray();
        var artists = await VisibleArtists(now)
            .Where(a => a.StudioId != null && a.ModerationStatus == ArtistModerationStatus.Active && studioIds.Contains(a.StudioId.Value))
            .Select(a => new { StudioId = a.StudioId!.Value, a.Id, a.FirstName, a.LastName })
            .ToListAsync(cancellationToken);
        var artistIds = artists.Select(a => a.Id).ToArray();
        var styles = await context.ArtistSpecialtyStyles
            .AsNoTracking()
            .Where(s => artistIds.Contains(s.TattooArtistId))
            .Select(s => new StyleRow(s.TattooArtistId, s.Name))
            .ToListAsync(cancellationToken);

        var stylesByArtist = styles
            .GroupBy(s => s.ArtistId)
            .ToDictionary(g => g.Key, g => string.Join(" ", g.Select(x => x.Name)));
        var artistsByStudio = artists
            .GroupBy(a => a.StudioId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(" ", g.Select(a => $"{a.FirstName} {a.LastName} {stylesByArtist.GetValueOrDefault(a.Id)}")));

        return studioRows.Where(studio =>
        {
            var searchable = StudioService.NormalizeSearch(string.Join(" ", new[]
            {
                studio.Name, studio.City, studio.Country, studio.Address,
                artistsByStudio.GetValueOrDefault(studio.Id)
            }));
            return tokens.All(token => searchable.Contains(token, StringComparison.Ordinal));
        }).ToList();
    }

    private async Task<List<StudioDto>> BuildDtosAsync(
        IReadOnlyCollection<StudioRow> studioRows,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (studioRows.Count == 0) return [];

        var started = System.Diagnostics.Stopwatch.StartNew();
        var studioIds = studioRows.Select(s => s.Id).ToArray();
        var artists = await VisibleArtists(now)
            .Where(a => a.StudioId != null && a.ModerationStatus == ArtistModerationStatus.Active && studioIds.Contains(a.StudioId.Value))
            .Select(a => new ArtistRow(
                a.Id, a.StudioId!.Value, a.FirstName, a.LastName,
                a.User.ProfileImageUrl, a.Description,
                a.ShowPhoneNumberOnPublicProfile ? a.PhoneNumber : string.Empty,
                a.IsVerified, a.JoinedStudioOn))
            .ToListAsync(cancellationToken);

        var artistIds = artists.Select(a => a.Id).ToArray();
        var ratings = await context.ArtistReviews
            .AsNoTracking()
            .Where(r => artistIds.Contains(r.TattooArtistId))
            .GroupBy(r => r.TattooArtistId)
            .Select(g => new RatingRow(g.Key, g.Count(), g.Average(r => r.Rating)))
            .ToListAsync(cancellationToken);
        var portfolio = await context.Set<PortfolioImage>()
            .AsNoTracking()
            .Where(p => artistIds.Contains(p.TattooArtistId))
            .OrderBy(p => p.Id)
            .Select(p => new PortfolioRow(p.TattooArtistId, p.ImageUrl))
            .ToListAsync(cancellationToken);
        var styles = await context.ArtistSpecialtyStyles
            .AsNoTracking()
            .Where(s => artistIds.Contains(s.TattooArtistId))
            .OrderBy(s => s.Name)
            .Select(s => new StyleRow(s.TattooArtistId, s.Name))
            .ToListAsync(cancellationToken);

        var ratingByArtist = ratings.ToDictionary(r => r.ArtistId);
        var portfolioByArtist = portfolio.ToLookup(p => p.ArtistId);
        var stylesByArtist = styles.ToLookup(s => s.ArtistId);
        var artistsByStudio = artists.ToLookup(a => a.StudioId);

        var result = new List<StudioDto>(studioRows.Count);
        foreach (var studio in studioRows)
        {
            var artistDtos = artistsByStudio[studio.Id]
                .OrderBy(a => a.Id == studio.OwnerArtistId ? 0 : 1)
                .ThenBy(a => a.JoinedStudioOn ?? DateTime.MaxValue)
                .ThenBy(a => a.Id)
                .Select(a =>
                {
                    ratingByArtist.TryGetValue(a.Id, out var rating);
                    return new StudioArtistDto
                    {
                        Id = a.Id,
                        FirstName = a.FirstName,
                        LastName = a.LastName,
                        ProfileImageUrl = CreateMediaUrl(a.ProfileImageUrl),
                        Description = a.Description,
                        PhoneNumber = a.PhoneNumber,
                        IsVerified = a.IsVerified,
                        AverageRating = rating == null ? 0 : Math.Round(rating.Average, 1),
                        ReviewCount = rating?.Count ?? 0,
                        JoinedStudioOn = a.JoinedStudioOn,
                        PortfolioImageUrls = portfolioByArtist[a.Id]
                            .Select(p => mediaUrls.CreateReadUrl(p.ImageUrl)).ToList(),
                        SpecialtyStyles = stylesByArtist[a.Id].Select(s => s.Name).ToList()
                    };
                })
                .ToList();

            var reviewCount = artistDtos.Sum(a => a.ReviewCount);
            var weightedRating = reviewCount == 0
                ? (double?)null
                : Math.Round(artistDtos.Sum(a => a.AverageRating * a.ReviewCount) / reviewCount, 1);
            result.Add(new StudioDto
            {
                Id = studio.Id,
                Name = studio.Name,
                Description = studio.Description,
                Address = studio.Address,
                City = studio.City,
                Country = studio.Country,
                Latitude = studio.Latitude,
                Longitude = studio.Longitude,
                IsOpenForJoinRequests = studio.IsOpenForJoinRequests,
                CoverImageUrl = CreateMediaUrl(studio.CoverImageUrl),
                LogoImageUrl = CreateMediaUrl(studio.LogoImageUrl),
                ArtistCount = artistDtos.Count,
                AverageRating = weightedRating,
                ReviewCount = reviewCount,
                SpecialtyStyles = artistDtos.SelectMany(a => a.SpecialtyStyles)
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                Artists = artistDtos,
                PortfolioPreviewUrls = artistDtos.SelectMany(a => a.PortfolioImageUrls).Take(12).ToList()
            });
        }

        logger.LogInformation(
            "Built {StudioCount} public studio DTOs with {ArtistCount} artists in {ElapsedMs}ms.",
            result.Count, artists.Count, started.ElapsedMilliseconds);
        return result;
    }

    private string? CreateMediaUrl(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : mediaUrls.CreateReadUrl(path);

    private sealed record StudioRow(
        int Id, int? OwnerArtistId, string Name, string Description, string Address,
        string City, string Country, double? Latitude, double? Longitude,
        bool IsOpenForJoinRequests, string? CoverImageUrl, string? LogoImageUrl);
    private sealed record ArtistRow(
        int Id, int StudioId, string FirstName, string LastName, string? ProfileImageUrl,
        string Description, string PhoneNumber, bool IsVerified, DateTime? JoinedStudioOn);
    private sealed record RatingRow(int ArtistId, int Count, double Average);
    private sealed record PortfolioRow(int ArtistId, string ImageUrl);
    private sealed record StyleRow(int ArtistId, string Name);
}
