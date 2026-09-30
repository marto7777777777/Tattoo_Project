using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.StudioDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class ClientFavoriteStudioService(TattooDbContext context, TimeProvider timeProvider, StudioReadService studioReader) : IClientFavoriteStudioService
    {
        public async Task<ResultService> AddAsync(int studioId, string userId)
        {
            var clientId = await context.Clients
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (int?)x.Id)
                .SingleOrDefaultAsync();
            if (clientId == null) return ResultService.Fail("Client profile not found.");
            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (!await context.Studios.AsNoTracking().AnyAsync(x =>
                    x.Id == studioId && x.Artists.Any(a => a.Subscription != null &&
                        ((a.Subscription.Status == ArtistSubscriptionStatuses.Trialing && a.Subscription.TrialEndsAt > now) ||
                         (a.Subscription.Status == ArtistSubscriptionStatuses.GracePeriod && a.Subscription.CurrentPeriodEndsAt > now) ||
                         (a.Subscription.Status == ArtistSubscriptionStatuses.Active && a.Subscription.CurrentPeriodEndsAt > now)))))
                return ResultService.Fail("Studio not found.");
            if (await context.ClientFavoriteStudios.AnyAsync(x => x.ClientId == clientId.Value && x.StudioId == studioId))
                return ResultService.Fail("This studio is already in your favorites.");

            context.ClientFavoriteStudios.Add(new ClientFavoriteStudio
            {
                ClientId = clientId.Value,
                StudioId = studioId,
                CreatedOn = timeProvider.GetUtcNow().UtcDateTime
            });
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> RemoveAsync(int studioId, string userId)
        {
            var clientId = await context.Clients
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (int?)x.Id)
                .SingleOrDefaultAsync();
            if (clientId == null) return ResultService.Fail("Client profile not found.");
            var favorite = await context.ClientFavoriteStudios
                .FirstOrDefaultAsync(x => x.ClientId == clientId.Value && x.StudioId == studioId);
            if (favorite == null) return ResultService.Fail("This studio is not in your favorites.");

            context.ClientFavoriteStudios.Remove(favorite);
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService<ICollection<StudioDto>>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
        {
            var clientId = await context.Clients
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (int?)x.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (clientId == null)
                return ResultService<ICollection<StudioDto>>.Fail("Client profile not found.");

            var favoriteStudioIds = await context.ClientFavoriteStudios
                .AsNoTracking()
                .Where(x => x.ClientId == clientId.Value)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => x.StudioId)
                .ToListAsync(cancellationToken);

            var studios = await studioReader.GetPublicStudiosByIdAsync(favoriteStudioIds, cancellationToken);
            return ResultService<ICollection<StudioDto>>.Ok(studios);
        }

        public async Task<ResultService<ICollection<int>>> GetMineIdsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var clientId = await context.Clients
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => (int?)x.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (clientId == null)
                return ResultService<ICollection<int>>.Fail("Client profile not found.");

            var ids = await context.ClientFavoriteStudios
                .AsNoTracking()
                .Where(x => x.ClientId == clientId.Value)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => x.StudioId)
                .ToListAsync(cancellationToken);
            return ResultService<ICollection<int>>.Ok(ids);
        }
    }
}
