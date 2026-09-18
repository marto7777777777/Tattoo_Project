using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.AdminDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public class AdminService(
    TattooDbContext context,
    UserManager<ApplicationUser> userManager,
    IFileStorage storage,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<AdminService> logger) : IAdminService
{
    public async Task<ResultService<AdminOverviewDto>> GetOverviewAsync()
    {
        var dto = new AdminOverviewDto
        {
            Users = await userManager.Users.CountAsync(),
            Clients = await context.Clients.CountAsync(),
            Artists = await context.TattooArtists.CountAsync(),
            TattooRequests = await context.TattooRequests.CountAsync(),
            AiProjects = await context.AiTattooProjects.CountAsync()
        };

        return ResultService<AdminOverviewDto>.Ok(dto);
    }

    public async Task<ResultService<ICollection<AdminUserDto>>> GetUsersAsync()
    {
        var users = await userManager.Users
            .OrderBy(x => x.Email)
            .ToListAsync();

        var clientProfiles = await context.Clients
            .Select(x => new { x.Id, x.UserId })
            .ToDictionaryAsync(x => x.UserId, x => x.Id);

        var artistProfiles = await context.TattooArtists
            .Select(x => new { x.Id, x.UserId })
            .ToDictionaryAsync(x => x.UserId, x => x.Id);

        var clientRequestCounts = await context.TattooRequests
            .GroupBy(x => x.Client.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var artistRequestCounts = await context.TattooRequests
            .GroupBy(x => x.TattooArtist.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var aiCounts = await context.AiTattooProjects
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var result = new List<AdminUserDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            clientRequestCounts.TryGetValue(user.Id, out var clientCount);
            artistRequestCounts.TryGetValue(user.Id, out var artistCount);
            aiCounts.TryGetValue(user.Id, out var aiCount);

            result.Add(new AdminUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList(),
                ClientProfileId = clientProfiles.TryGetValue(user.Id, out var clientProfileId) ? clientProfileId : null,
                ArtistProfileId = artistProfiles.TryGetValue(user.Id, out var artistProfileId) ? artistProfileId : null,
                TattooRequestCount = clientCount + artistCount,
                AiProjectCount = aiCount
            });
        }

        return ResultService<ICollection<AdminUserDto>>.Ok(result);
    }

    public async Task<ResultService<ICollection<AdminTattooRequestDto>>> GetTattooRequestsAsync()
    {
        var requests = await context.TattooRequests
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.TattooArtist)
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new AdminTattooRequestDto
            {
                Id = x.Id,
                ClientName = x.Client.FirstName + " " + x.Client.LastName,
                ArtistName = x.TattooArtist.FirstName + " " + x.TattooArtist.LastName,
                Placement = x.Placement,
                TattooStyle = x.TattooStyle,
                Status = x.Status.ToString(),
                CreatedOn = x.CreatedOn
            })
            .ToListAsync();

        return ResultService<ICollection<AdminTattooRequestDto>>.Ok(requests);
    }


    public async Task<ResultService<ICollection<AdminAiProjectDto>>> GetAiProjectsAsync()
    {
        var projects = await context.AiTattooProjects
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Versions)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new AdminAiProjectDto
            {
                Id = x.Id,
                UserEmail = x.User.Email ?? string.Empty,
                Title = x.Title,
                TattooStyle = x.TattooStyle,
                Placement = x.Placement,
                VersionCount = x.Versions.Count,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return ResultService<ICollection<AdminAiProjectDto>>.Ok(projects);
    }

    public Task<ResultService> DeleteUserAsync(string userId, string currentAdminUserId)
        => DeleteUserCoreAsync(userId, currentAdminUserId, null);

    public Task<ResultService> DeleteUserForDeletionWorkflowAsync(string userId, Guid deletionRequestId)
        => DeleteUserCoreAsync(userId, "self-service-account-deletion", deletionRequestId);

    private async Task<ResultService> DeleteUserCoreAsync(string userId, string currentAdminUserId, Guid? deletionRequestId)
    {
        if (userId == currentAdminUserId)
            return ResultService.Fail("You cannot delete the admin account you are currently using.");

        var user = await userManager.FindByIdAsync(userId);
        if (user == null && !deletionRequestId.HasValue)
            return ResultService.Fail("User was not found.");
        if (user != null && await userManager.IsInRoleAsync(user, UserRoles.Admin))
            return ResultService.Fail("Admin accounts cannot be deleted from the admin panel.");
        if (user == null && deletionRequestId.HasValue)
        {
            var durableState = await context.AccountDeletionRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == deletionRequestId.Value);
            if (durableState == null || durableState.UserId != userId) return ResultService.Fail("Account deletion workflow state is invalid.");
            if ((int)durableState.State >= (int)AccountDeletionState.LocalDeletionCompleted) return ResultService.Ok();
            if (durableState.State != AccountDeletionState.ExternalSubscriptionsClosed) return ResultService.Fail("Account deletion workflow state is invalid.");
            // Recovery is allowed to continue deleting business rows by UserId even when the
            // Identity row disappeared in an older interrupted deployment.
        }

        // Provider HTTP calls are deliberately outside the SQL transaction.
        // Self-service deletion closes them before entering this method; admin deletion does it here.
        if (!deletionRequestId.HasValue)
        {
            var subscription = await context.ArtistSubscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.TattooArtist.UserId == userId);
            var externalClosure = await CloseExternalBillingForDeletionAsync(subscription);
            if (!externalClosure.Success) return externalClosure;
        }

        var deferredMedia = new List<string>();
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            if (deletionRequestId.HasValue)
            {
                await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:AccountDeletion:{deletionRequestId.Value:N}","account_deletion_concurrency_conflict","Account deletion is already being processed.");
                var workflow = await context.AccountDeletionRequests.FirstOrDefaultAsync(x => x.Id == deletionRequestId.Value);
                if (workflow == null || workflow.UserId != userId || (int)workflow.State < (int)AccountDeletionState.ExternalSubscriptionsClosed)
                {
                    await transaction.RollbackAsync();
                    return ResultService.Fail("Account deletion workflow state is invalid.");
                }
                if ((int)workflow.State >= (int)AccountDeletionState.LocalDeletionCompleted)
                {
                    await transaction.CommitAsync();
                    return ResultService.Ok();
                }
            }

            var client = await context.Clients.FirstOrDefaultAsync(x => x.UserId == userId);
            if (client != null)
            {
                var result = await DeleteClientProfileInternalAsync(client.Id, removeRole: false, deferredMedia);
                if (!result.Success) { await transaction.RollbackAsync(); return result; }
            }

            var artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.UserId == userId);
            if (artist != null)
            {
                var result = await DeleteArtistProfileInternalAsync(artist.Id, removeRole: false, deferredMedia);
                if (!result.Success) { await transaction.RollbackAsync(); return result; }
            }

            await context.EmailVerificationCodes.Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await context.PendingEmailChanges.Where(x => x.UserId == userId).ExecuteDeleteAsync();

            var aiProjectIds = await context.AiTattooProjects.Where(x => x.UserId == userId).Select(x => x.Id).ToListAsync();
            foreach (var aiProjectId in aiProjectIds)
                await DeleteAiProjectGraphAsync(aiProjectId, deferredMedia);

            var pseudonym = "deleted:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId)))[..32].ToLowerInvariant();
            await context.AiProjectStorePurchases.Where(x => x.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UserId, pseudonym));

            if (!string.IsNullOrWhiteSpace(user?.ProfileImageUrl)) deferredMedia.Add(user.ProfileImageUrl);

            // Transactional outbox for physical media cleanup. If the process dies immediately
            // after the DB commit, the FileCleanupHostedService still owns these tasks.
            var now = timeProvider.GetUtcNow().UtcDateTime;
            foreach (var key in deferredMedia.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal))
            {
                context.FileCleanupTasks.Add(new FileCleanupTask
                {
                    StorageKey = key,
                    Reason = "account_deletion",
                    RetryCount = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (user != null)
            {
                user.TokenVersion++;
                var identityResult = await userManager.DeleteAsync(user);
                if (!identityResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return ResultService.Fail(string.Join(" ", identityResult.Errors.Select(x => x.Description)));
                }
            }

            if (deletionRequestId.HasValue)
            {
                var workflow = await context.AccountDeletionRequests.FirstAsync(x => x.Id == deletionRequestId.Value);
                workflow.State = AccountDeletionState.LocalDeletionCompleted;
                workflow.LastErrorCode = null;
                workflow.UpdatedAt = now;
                await context.SaveChangesAsync();
            }
            else
            {
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        await CleanupDeferredFilesAsync(deferredMedia, "account_deletion");
        return ResultService.Ok();
    }

    public Task<ResultService> DeleteClientProfileAsync(int clientId)
        => DeleteClientProfileInternalAsync(clientId, removeRole: true);

    public Task<ResultService> DeleteArtistProfileAsync(int artistId)
        => DeleteArtistProfileInternalAsync(artistId, removeRole: true);

    public async Task<ResultService> DeleteTattooRequestAsync(int tattooRequestId)
    {
        var exists = await context.TattooRequests.AnyAsync(x => x.Id == tattooRequestId);
        if (!exists) return ResultService.Fail("Tattoo request was not found.");

        await DeleteTattooRequestGraphAsync(tattooRequestId);
        return ResultService.Ok();
    }

    public async Task<ResultService> DeleteAiProjectAsync(int projectId)
    {
        var exists = await context.AiTattooProjects.AnyAsync(x => x.Id == projectId);
        if (!exists) return ResultService.Fail("AI tattoo project was not found.");

        await DeleteAiProjectGraphAsync(projectId);
        return ResultService.Ok();
    }


    public async Task<ResultService> SetArtistVerifiedAsync(int artistId, bool isVerified)
    {
        var artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.Id == artistId);
        if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");

        artist.IsVerified = isVerified;
        await context.SaveChangesAsync();
        return ResultService.Ok();
    }

    private async Task<ResultService> DeleteClientProfileInternalAsync(int clientId, bool removeRole, List<string>? deferredMedia = null)
    {
        var client = await context.Clients.FirstOrDefaultAsync(x => x.Id == clientId);
        if (client == null) return ResultService.Fail("Client profile was not found.");

        var requestIds = await context.TattooRequests
            .Where(x => x.ClientId == clientId)
            .Select(x => x.Id)
            .ToListAsync();

        foreach (var requestId in requestIds)
            await DeleteTattooRequestGraphAsync(requestId, deferredMedia);

        await context.ArtistReviews.Where(x => x.ClientId == clientId).ExecuteDeleteAsync();
        await context.ClientFavoriteStudios.Where(x => x.ClientId == clientId).ExecuteDeleteAsync();

        context.Clients.Remove(client);
        await context.SaveChangesAsync();

        if (removeRole)
        {
            var user = await userManager.FindByIdAsync(client.UserId);
            if (user != null && await userManager.IsInRoleAsync(user, UserRoles.Client))
                await userManager.RemoveFromRoleAsync(user, UserRoles.Client);
        }

        return ResultService.Ok();
    }

    private async Task<ResultService> DeleteArtistProfileInternalAsync(int artistId, bool removeRole, List<string>? deferredMedia = null)
    {
        var artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.Id == artistId);
        if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");

        var ownedStudio = await context.Studios.FirstOrDefaultAsync(s => s.OwnerArtistId == artistId);
        if (ownedStudio != null)
        {
            var nextOwner = await context.TattooArtists
                .Where(a => a.StudioId == ownedStudio.Id && a.Id != artistId)
                .OrderBy(a => a.JoinedStudioOn ?? DateTime.MaxValue)
                .ThenBy(a => a.Id)
                .FirstOrDefaultAsync();

            if (nextOwner != null)
            {
                ownedStudio.OwnerArtistId = nextOwner.Id;
            }
            else
            {
                var studioCover = ownedStudio.CoverImageUrl;
                var studioLogo = ownedStudio.LogoImageUrl;
                ownedStudio.OwnerArtistId = null;
                artist.StudioId = null;
                await context.SaveChangesAsync();
                context.Studios.Remove(ownedStudio);
                await context.SaveChangesAsync();
                if (!string.IsNullOrWhiteSpace(studioCover)) { if (deferredMedia != null) deferredMedia.Add(studioCover); else await storage.DeleteAsync(studioCover); }
                if (!string.IsNullOrWhiteSpace(studioLogo)) { if (deferredMedia != null) deferredMedia.Add(studioLogo); else await storage.DeleteAsync(studioLogo); }
            }
        }

        var requestIds = await context.TattooRequests
            .Where(x => x.TattooArtistId == artistId)
            .Select(x => x.Id)
            .ToListAsync();

        foreach (var requestId in requestIds)
            await DeleteTattooRequestGraphAsync(requestId, deferredMedia);

        await context.ArtistReviews.Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();
        await context.ArtistUnavailableDates.Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();
        await context.Schedules.Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();
        await context.Set<ArtistRequirement>().Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();
        var portfolioMedia = await context.Set<PortfolioImage>()
            .Where(x => x.TattooArtistId == artistId)
            .Select(x => x.ImageUrl)
            .ToListAsync();
        await context.Set<PortfolioImage>().Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();
        await context.AnalyticsOutboxEvents.Where(x => x.TattooArtistId == artistId).ExecuteDeleteAsync();

        context.TattooArtists.Remove(artist);
        await context.SaveChangesAsync();

        foreach (var media in portfolioMedia)
        {
            if (deferredMedia != null) deferredMedia.Add(media); else await storage.DeleteAsync(media);
        }

        if (removeRole)
        {
            var user = await userManager.FindByIdAsync(artist.UserId);
            if (user != null && await userManager.IsInRoleAsync(user, UserRoles.TattooArtist))
                await userManager.RemoveFromRoleAsync(user, UserRoles.TattooArtist);
        }

        return ResultService.Ok();
    }


    private async Task DeleteAiProjectGraphAsync(int projectId, List<string>? deferredMedia = null)
    {
        var project = await context.AiTattooProjects
            .AsNoTracking()
            .Where(x => x.Id == projectId)
            .Select(x => new { x.InitialReferenceImageUrl })
            .FirstOrDefaultAsync();
        var versionMedia = await context.AiTattooVersions
            .AsNoTracking()
            .Where(x => x.AiTattooProjectId == projectId)
            .Select(x => x.ImageUrl)
            .ToListAsync();

        // Anti-replay store purchase rows use SET NULL and intentionally survive project deletion.
        await context.AiTattooVersions
            .Where(x => x.AiTattooProjectId == projectId && x.ParentVersionId != null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ParentVersionId, (int?)null));

        await context.AiProjectPayments.Where(x => x.AiTattooProjectId == projectId).ExecuteDeleteAsync();
        await context.AiTattooVersions.Where(x => x.AiTattooProjectId == projectId).ExecuteDeleteAsync();
        await context.AiTattooProjects.Where(x => x.Id == projectId).ExecuteDeleteAsync();

        if (!string.IsNullOrWhiteSpace(project?.InitialReferenceImageUrl))
        {
            if (deferredMedia != null) deferredMedia.Add(project.InitialReferenceImageUrl); else await storage.DeleteAsync(project.InitialReferenceImageUrl);
        }
        foreach (var media in versionMedia)
        {
            if (deferredMedia != null) deferredMedia.Add(media); else await storage.DeleteAsync(media);
        }
    }

    private async Task DeleteTattooRequestGraphAsync(int tattooRequestId, List<string>? deferredMedia = null)
    {
        var referenceMedia = await context.TattooReferenceImages
            .AsNoTracking()
            .Where(x => x.TattooRequestId == tattooRequestId)
            .Select(x => x.ImageUrl)
            .ToListAsync();

        await context.ArtistReviews.Where(x => x.TattooRequestId == tattooRequestId).ExecuteDeleteAsync();
        await context.TattooSessions.Where(x => x.TattooRequestId == tattooRequestId).ExecuteDeleteAsync();
        await context.Consultations.Where(x => x.TattooRequestId == tattooRequestId).ExecuteDeleteAsync();
        await context.ArtistResponses.Where(x => x.TattooRequestId == tattooRequestId).ExecuteDeleteAsync();
        await context.TattooReferenceImages.Where(x => x.TattooRequestId == tattooRequestId).ExecuteDeleteAsync();
        await context.TattooRequests.Where(x => x.Id == tattooRequestId).ExecuteDeleteAsync();

        foreach (var media in referenceMedia)
        {
            if (deferredMedia != null) deferredMedia.Add(media); else await storage.DeleteAsync(media);
        }
    }
    private async Task<ResultService> CloseExternalBillingForDeletionAsync(ArtistSubscription? subscription)
    {
        if (subscription == null) return ResultService.Ok();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (subscription.ActiveProvider is SubscriptionProviders.GooglePlay or SubscriptionProviders.Apple &&
            SubscriptionEntitlementRules.HasAccess(subscription, now) && !subscription.CancelAtPeriodEnd)
            return ResultService.Fail("The active App Store or Google Play subscription must be cancelled before account deletion.");

        if (string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId) && string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
            return ResultService.Ok();
        var secret = configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
            return ResultService.Fail("Stripe account deletion cannot be completed because billing is not configured.");
        StripeConfiguration.ApiKey = secret;
        try
        {
            if (!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
                await new SubscriptionService().CancelAsync(subscription.StripeSubscriptionId);
            if (!string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
                await new CustomerService().DeleteAsync(subscription.StripeCustomerId);
            return ResultService.Ok();
        }
        catch (StripeException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return ResultService.Ok();
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Admin account-deletion billing closure failed; deletion remains retryable.");
            return ResultService.Fail("Billing closure could not be completed. Retry account deletion.");
        }
    }

    private async Task CleanupDeferredFilesAsync(IEnumerable<string> media, string reason)
    {
        foreach (var key in media.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal))
        {
            var task = await context.FileCleanupTasks
                .Where(x => x.CompletedAt == null && x.StorageKey == key && x.Reason == reason)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();
            if (task == null)
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                task = new FileCleanupTask { StorageKey = key, Reason = reason, CreatedAt = now, UpdatedAt = now };
                context.FileCleanupTasks.Add(task);
                try { await context.SaveChangesAsync(); }
                catch (Exception persistenceError)
                {
                    logger.LogCritical(persistenceError,"Failed to persist deferred file cleanup task for reason {Reason}; physical media may require manual reconciliation.",reason);
                    continue;
                }
            }

            try
            {
                await storage.DeleteAsync(key);
                task.CompletedAt = timeProvider.GetUtcNow().UtcDateTime;
                task.UpdatedAt = task.CompletedAt.Value;
                task.LastError = null;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                task.RetryCount++;
                task.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
                task.LastError = ex.GetType().Name;
                try { await context.SaveChangesAsync(); }
                catch (Exception persistenceError)
                {
                    logger.LogCritical(persistenceError,"Failed to persist file cleanup retry state for cleanup task {CleanupTaskId}.",task.Id);
                }
                logger.LogWarning(ex,"Post-commit physical file deletion failed for cleanup task {CleanupTaskId}; it remains retryable.",task.Id);
            }
        }
    }

}
