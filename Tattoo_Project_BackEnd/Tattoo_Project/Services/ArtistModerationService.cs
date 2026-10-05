using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs;
using Tattoo_Project.DTOs.AdminDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public sealed class ArtistModerationService(
    TattooDbContext context,
    UserManager<ApplicationUser> userManager,
    IImageSanitizer imageSanitizer,
    IFileStorage storage,
    IPrivateMediaUrlService mediaUrls,
    TimeProvider timeProvider) : IArtistModerationService
{
    private const long MaxImageSize = 5 * 1024 * 1024;

    private static readonly string[] AllowedReasons =
    [
        "Inappropriate content",
        "Harassment or abusive behavior",
        "Spam or misleading profile",
        "Copyright or intellectual property",
        "Other"
    ];

    public async Task<ResultService> CreateReportAsync(string reporterUserId, int artistId, CreateArtistReportDto dto)
    {
        var reporter = await userManager.FindByIdAsync(reporterUserId);
        if (reporter == null) return ResultService.Fail("User was not found.");

        var artist = await context.TattooArtists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == artistId);
        if (artist == null || artist.ModerationStatus != ArtistModerationStatus.Active)
            return ResultService.Fail("The artist profile is not available.");
        if (artist.UserId == reporterUserId)
            return ResultService.Fail("You cannot report your own artist profile.");

        var roles = await userManager.GetRolesAsync(reporter);
        if (!roles.Contains(UserRoles.Client) && !roles.Contains(UserRoles.TattooArtist) && !roles.Contains(UserRoles.Admin))
            return ResultService.Fail("Only clients and artists can submit artist reports.");

        var reason = dto.Reason?.Trim() ?? string.Empty;
        if (!AllowedReasons.Contains(reason, StringComparer.Ordinal))
            return ResultService.Fail("Please select a valid report reason.");
        var description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        if (description?.Length > 2000) return ResultService.Fail("Report details are too long.");

        var alreadyPending = await context.ArtistReports.AnyAsync(x =>
            x.ReporterUserId == reporterUserId && x.ReportedArtistId == artistId && x.Status == ArtistReportStatus.Pending);
        if (alreadyPending) return ResultService.Fail("You already have a report under review for this artist.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        context.ArtistReports.Add(new ArtistReport
        {
            ReporterUserId = reporterUserId,
            ReportedArtistId = artistId,
            Reason = reason,
            Description = description,
            Status = ArtistReportStatus.Pending,
            CreatedAtUtc = now
        });
        await context.SaveChangesAsync();
        return ResultService.Ok();
    }

    public async Task<ResultService<ArtistModerationStatusDto>> GetMyStatusAsync(string userId)
    {
        var artist = await context.TattooArtists.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        if (artist == null) return ResultService<ArtistModerationStatusDto>.Fail("Artist profile was not found.");

        var submission = await context.ArtistModerationSubmissions.AsNoTracking()
            .Include(x => x.Images)
            .Where(x => x.TattooArtistId == artist.Id && (x.Status == ArtistModerationSubmissionStatus.Draft || x.Status == ArtistModerationSubmissionStatus.Pending))
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        return ResultService<ArtistModerationStatusDto>.Ok(new ArtistModerationStatusDto
        {
            Status = artist.ModerationStatus.ToString(),
            PendingSubmissionId = submission?.Id,
            Images = submission?.Images.OrderBy(x => x.Id).Select(x => new ModerationImageDto { Id = x.Id, ImageUrl = mediaUrls.CreateReadUrl(x.ImageUrl) }).ToList() ?? [],
            Message = artist.ModerationStatus switch
            {
                ArtistModerationStatus.Blocked => "Your artist profile is currently blocked by InkRoute moderation. You can submit updated portfolio content for review.",
                ArtistModerationStatus.PendingReinstatement => "Your updated artist profile content is currently under admin review.",
                _ => null
            }
        });
    }

    public async Task<ResultService<ProfileModerationUploadDto>> AddReinstatementImageAsync(string userId, IFormFile image)
    {
        var artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.UserId == userId);
        if (artist == null) return ResultService<ProfileModerationUploadDto>.Fail("Artist profile was not found.");
        if (artist.ModerationStatus == ArtistModerationStatus.Active)
            return ResultService<ProfileModerationUploadDto>.Fail("Your artist profile is active. Upload portfolio images normally.");
        if (artist.ModerationStatus == ArtistModerationStatus.PendingReinstatement)
            return ResultService<ProfileModerationUploadDto>.Fail("Your reinstatement submission is already under review.");

        var submission = await context.ArtistModerationSubmissions
            .Include(x => x.Images)
            .Where(x => x.TattooArtistId == artist.Id && x.Status == ArtistModerationSubmissionStatus.Draft)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (submission == null)
        {
            var sourceReportId = await context.ArtistReports
                .Where(x => x.ReportedArtistId == artist.Id && x.Status == ArtistReportStatus.Blocked)
                .OrderByDescending(x => x.ReviewedAtUtc ?? x.CreatedAtUtc)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();
            submission = new ArtistModerationSubmission
            {
                TattooArtistId = artist.Id,
                SourceReportId = sourceReportId,
                Status = ArtistModerationSubmissionStatus.Draft,
                CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
            };
            context.ArtistModerationSubmissions.Add(submission);
            await context.SaveChangesAsync();
        }

        if (submission.Images.Count >= 20)
            return ResultService<ProfileModerationUploadDto>.Fail("A moderation submission can contain up to 20 images.");

        var sanitized = await imageSanitizer.SanitizeAsync(image, MaxImageSize, 30_000_000);
        if (!sanitized.Success) return ResultService<ProfileModerationUploadDto>.Fail(sanitized.ErrorMessage!);

        var imageKey = await storage.SaveAsync(sanitized.Data!.Bytes, "moderation-portfolio", sanitized.Data.Extension, StoredFileVisibility.Private);
        var pendingImage = new ArtistModerationSubmissionImage { SubmissionId = submission.Id, ImageUrl = imageKey };
        context.ArtistModerationSubmissionImages.Add(pendingImage);
        try
        {
            await context.SaveChangesAsync();
        }
        catch
        {
            await storage.DeleteAsync(imageKey);
            throw;
        }

        return ResultService<ProfileModerationUploadDto>.Ok(new ProfileModerationUploadDto
        {
            SubmissionId = submission.Id,
            ImageId = pendingImage.Id,
            ImageUrl = mediaUrls.CreateReadUrl(imageKey),
            Status = submission.Status.ToString()
        });
    }

    public async Task<ResultService> DeleteReinstatementImageAsync(string userId, int imageId)
    {
        var image = await context.ArtistModerationSubmissionImages
            .Include(x => x.Submission)
            .ThenInclude(x => x.TattooArtist)
            .FirstOrDefaultAsync(x => x.Id == imageId && x.Submission.TattooArtist.UserId == userId);
        if (image == null) return ResultService.Fail("Moderation image was not found.");
        if (image.Submission.Status != ArtistModerationSubmissionStatus.Draft)
            return ResultService.Fail("This moderation submission can no longer be edited.");

        var key = image.ImageUrl;
        context.Remove(image);
        await context.SaveChangesAsync();
        await storage.DeleteAsync(key);
        return ResultService.Ok();
    }

    public async Task<ResultService> SubmitReinstatementAsync(string userId)
    {
        var artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.UserId == userId);
        if (artist == null) return ResultService.Fail("Artist profile was not found.");
        if (artist.ModerationStatus != ArtistModerationStatus.Blocked)
            return ResultService.Fail("Your artist profile is not currently blocked.");

        var submission = await context.ArtistModerationSubmissions
            .Include(x => x.Images)
            .Where(x => x.TattooArtistId == artist.Id && x.Status == ArtistModerationSubmissionStatus.Draft)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();
        if (submission == null || submission.Images.Count == 0)
            return ResultService.Fail("Add at least one updated portfolio image before submitting for review.");

        submission.Status = ArtistModerationSubmissionStatus.Pending;
        artist.ModerationStatus = ArtistModerationStatus.PendingReinstatement;
        await context.SaveChangesAsync();
        return ResultService.Ok();
    }

    public async Task<ResultService<ICollection<AdminArtistReportDto>>> GetReportsAsync()
    {
        var reports = await context.ArtistReports.AsNoTracking()
            .Include(x => x.ReportedArtist).ThenInclude(x => x.User)
            .Include(x => x.ReporterUser)
            .OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new AdminArtistReportDto
            {
                Id = x.Id,
                ReportedArtistId = x.ReportedArtistId,
                ReportedArtistName = x.ReportedArtist.FirstName + " " + x.ReportedArtist.LastName,
                ReportedArtistEmail = x.ReportedArtist.User.Email,
                PublicProfileSlug = x.ReportedArtist.PublicProfileSlug,
                ReporterUserId = x.ReporterUserId,
                ReporterName = x.ReporterUser.FirstName + " " + x.ReporterUser.LastName,
                ReporterEmail = x.ReporterUser.Email ?? string.Empty,
                Reason = x.Reason,
                Description = x.Description,
                Status = x.Status.ToString(),
                DecisionNote = x.DecisionNote,
                CreatedAtUtc = x.CreatedAtUtc,
                ReviewedAtUtc = x.ReviewedAtUtc
            }).ToListAsync();
        return ResultService<ICollection<AdminArtistReportDto>>.Ok(reports);
    }

    public async Task<ResultService<ICollection<AdminModerationSubmissionDto>>> GetSubmissionsAsync()
    {
        var submissions = await context.ArtistModerationSubmissions.AsNoTracking()
            .Include(x => x.TattooArtist).ThenInclude(x => x.User)
            .Include(x => x.Images)
            .OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        var result = submissions.Select(x => new AdminModerationSubmissionDto
        {
            Id = x.Id,
            TattooArtistId = x.TattooArtistId,
            ArtistName = x.TattooArtist.FirstName + " " + x.TattooArtist.LastName,
            ArtistEmail = x.TattooArtist.User.Email ?? string.Empty,
            Status = x.Status.ToString(),
            SourceReportId = x.SourceReportId,
            AdminNote = x.AdminNote,
            CreatedAtUtc = x.CreatedAtUtc,
            ReviewedAtUtc = x.ReviewedAtUtc,
            ImageUrls = x.Images.OrderBy(i => i.Id).Select(i => mediaUrls.CreateReadUrl(i.ImageUrl)).ToList()
        }).ToList();
        return ResultService<ICollection<AdminModerationSubmissionDto>>.Ok(result);
    }

    public async Task<ResultService> ReviewReportAsync(int reportId, string adminUserId, ModerateArtistReportRequest request)
    {
        var report = await context.ArtistReports.Include(x => x.ReportedArtist).FirstOrDefaultAsync(x => x.Id == reportId);
        if (report == null) return ResultService.Fail("Report was not found.");
        if (report.Status != ArtistReportStatus.Pending) return ResultService.Fail("This report has already been reviewed.");

        var note = string.IsNullOrWhiteSpace(request.DecisionNote) ? null : request.DecisionNote.Trim();
        if (note?.Length > 2000) return ResultService.Fail("Decision note is too long.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        report.ReviewedByAdminId = adminUserId;
        report.ReviewedAtUtc = now;
        report.DecisionNote = note;
        if (request.BlockArtist)
        {
            report.Status = ArtistReportStatus.Blocked;
            report.ReportedArtist.ModerationStatus = ArtistModerationStatus.Blocked;
        }
        else
        {
            report.Status = ArtistReportStatus.Dismissed;
        }
        await context.SaveChangesAsync();
        return ResultService.Ok();
    }

    public async Task<ResultService> ReviewSubmissionAsync(int submissionId, string adminUserId, ModerateArtistSubmissionRequest request)
    {
        var submission = await context.ArtistModerationSubmissions
            .Include(x => x.Images)
            .Include(x => x.TattooArtist).ThenInclude(x => x.PortfolioImages)
            .FirstOrDefaultAsync(x => x.Id == submissionId);
        if (submission == null) return ResultService.Fail("Moderation submission was not found.");
        if (submission.Status != ArtistModerationSubmissionStatus.Pending)
            return ResultService.Fail("This moderation submission is not awaiting review.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var note = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();
        if (note?.Length > 2000) return ResultService.Fail("Admin note is too long.");

        if (!request.Approve)
        {
            submission.Status = ArtistModerationSubmissionStatus.Rejected;
            submission.AdminNote = note;
            submission.ReviewedByAdminId = adminUserId;
            submission.ReviewedAtUtc = now;
            submission.TattooArtist.ModerationStatus = ArtistModerationStatus.Blocked;
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        var oldKeys = submission.TattooArtist.PortfolioImages.Select(x => x.ImageUrl).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var newImages = submission.Images.OrderBy(x => x.Id).Select(x => new PortfolioImage
        {
            TattooArtistId = submission.TattooArtistId,
            ImageUrl = x.ImageUrl
        }).ToList();

        context.PortfolioImages.RemoveRange(submission.TattooArtist.PortfolioImages);
        context.PortfolioImages.AddRange(newImages);
        submission.Status = ArtistModerationSubmissionStatus.Approved;
        submission.AdminNote = note;
        submission.ReviewedByAdminId = adminUserId;
        submission.ReviewedAtUtc = now;
        submission.TattooArtist.ModerationStatus = ArtistModerationStatus.Active;
        await context.SaveChangesAsync();

        foreach (var key in oldKeys)
            await storage.DeleteAsync(key);

        return ResultService.Ok();
    }
}
