using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Tattoo_Project.Data;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public class AccountDeletionService(
    TattooDbContext db,
    UserManager<ApplicationUser> users,
    IAdminService admin,
    IConfiguration config,
    TimeProvider timeProvider,
    ILogger<AccountDeletionService> logger) : IAccountDeletionService
{
    public async Task<ResultService> DeleteAsync(string userId, string password, string confirmation)
    {
        if (confirmation != "DELETE") return ResultService.Fail("Type DELETE to confirm account deletion.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var deletion = await db.AccountDeletionRequests.SingleOrDefaultAsync(x => x.UserId == userId);
        var user = await users.FindByIdAsync(userId);

        if (deletion == null)
        {
            if (user == null) return ResultService.Fail("User was not found.");
            if (await users.IsInRoleAsync(user, UserRoles.Admin)) return ResultService.Fail("Admin accounts require manual deletion review.");
            if (!await users.CheckPasswordAsync(user, password)) return ResultService.Fail("Password is incorrect.");

            deletion = new AccountDeletionRequest
            {
                UserId = userId,
                State = AccountDeletionState.DeletionRequested,
                RequestedAt = now,
                UpdatedAt = now
            };
            db.AccountDeletionRequests.Add(deletion);
            await db.SaveChangesAsync();
        }
        else if (deletion.State == AccountDeletionState.DeletionRequested)
        {
            // Before local deletion begins the account still exists, so a retry must authenticate again.
            if (user == null) return ResultService.Fail("Account deletion state is inconsistent and requires review.");
            if (await users.IsInRoleAsync(user, UserRoles.Admin)) return ResultService.Fail("Admin accounts require manual deletion review.");
            if (!await users.CheckPasswordAsync(user, password)) return ResultService.Fail("Password is incorrect.");
        }


        if (deletion.State == AccountDeletionState.DeletionRequested)
        {
            var subscription = await db.ArtistSubscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.TattooArtist.UserId == userId);
            if (subscription?.ActiveProvider is SubscriptionProviders.GooglePlay or SubscriptionProviders.Apple &&
                SubscriptionEntitlementRules.HasAccess(subscription, now) && !subscription.CancelAtPeriodEnd)
            {
                deletion.LastErrorCode = "MOBILE_SUBSCRIPTION_STILL_ACTIVE";
                deletion.UpdatedAt = now;
                await db.SaveChangesAsync();
                return ResultService.Fail("Cancel the active App Store or Google Play subscription first. Store access may remain active until the end of the already-paid period.");
            }

            var closeExternal = await CloseStripeAsync(subscription);
            if (!closeExternal.Success)
            {
                deletion.RetryCount++;
                deletion.LastErrorCode = "STRIPE_CLOSURE_RETRYABLE";
                deletion.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
                await db.SaveChangesAsync();
                return closeExternal;
            }

            deletion.State = AccountDeletionState.ExternalSubscriptionsClosed;
            deletion.LastErrorCode = null;
            deletion.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync();
        }

        if (deletion.State == AccountDeletionState.ExternalSubscriptionsClosed)
        {
            var result = await admin.DeleteUserForDeletionWorkflowAsync(userId, deletion.Id);
            if (!result.Success)
            {
                deletion.RetryCount++;
                deletion.LastErrorCode = "LOCAL_DELETION_RETRYABLE";
                deletion.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
                await db.SaveChangesAsync();
                return ResultService.Fail("External billing closure succeeded, but local account deletion must be retried.");
            }

            db.ChangeTracker.Clear();
            deletion = await db.AccountDeletionRequests.FirstAsync(x => x.Id == deletion.Id);
            if ((int)deletion.State < (int)AccountDeletionState.LocalDeletionCompleted)
                return ResultService.Fail("Local account deletion did not reach a durable completed state.");
        }

        await CompleteDurableDeletionRequestAsync(deletion.Id);
        return ResultService.Ok();
    }

    private async Task CompleteDurableDeletionRequestAsync(Guid deletionId)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await DatabaseApplicationLock.AcquireAsync(db,$"InkRoute:AccountDeletion:{deletionId:N}","account_deletion_concurrency_conflict","Account deletion is already being finalized.");
        db.ChangeTracker.Clear();
        var deletion=await db.AccountDeletionRequests.FirstOrDefaultAsync(x=>x.Id==deletionId);
        if(deletion==null){await tx.RollbackAsync();throw new InvalidOperationException("Deletion request disappeared during finalization.");}
        if(deletion.State==AccountDeletionState.Completed){await tx.CommitAsync();return;}
        if(deletion.State!=AccountDeletionState.LocalDeletionCompleted){await tx.RollbackAsync();throw new InvalidOperationException("Deletion request cannot be completed before local deletion succeeds.");}

        var completedAt = timeProvider.GetUtcNow().UtcDateTime;
        deletion.State = AccountDeletionState.Completed;
        deletion.UserId = null;
        deletion.LastErrorCode = null;
        deletion.CompletedAt = completedAt;
        deletion.UpdatedAt = completedAt;
        db.AccountDeletionAudits.Add(new AccountDeletionAudit
        {
            DeletedAt = completedAt,
            RetentionNote = "Deletion audit retained for legal and security accountability; no user identifier is stored in this audit record."
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private async Task<ResultService> CloseStripeAsync(ArtistSubscription? subscription)
    {
        if (subscription == null || string.IsNullOrWhiteSpace(config["Stripe:SecretKey"])) return ResultService.Ok();

        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
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
            logger.LogWarning(ex, "Stripe account-deletion closure failed; deletion request remains retryable.");
            return ResultService.Fail("Billing closure could not be completed. The deletion request is saved and can be retried safely.");
        }
    }
}
