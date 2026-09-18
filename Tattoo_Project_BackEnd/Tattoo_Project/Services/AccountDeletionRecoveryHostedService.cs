using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services;

/// <summary>
/// Resumes durable account deletion after a process crash. It may finish the local phase only
/// after the authenticated workflow already reached ExternalSubscriptionsClosed.
/// </summary>
public sealed class AccountDeletionRecoveryHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<AccountDeletionRecoveryHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RecoverAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex,"Account deletion recovery failed; it will retry."); }
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    internal async Task RecoverAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TattooDbContext>();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var candidates = await db.AccountDeletionRequests.AsNoTracking()
            .Where(x => x.State == AccountDeletionState.ExternalSubscriptionsClosed || x.State == AccountDeletionState.LocalDeletionCompleted)
            .OrderBy(x => x.UpdatedAt)
            .Select(x => new { x.Id, x.UserId, x.State })
            .Take(50).ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            if (candidate.State == AccountDeletionState.ExternalSubscriptionsClosed)
            {
                if (string.IsNullOrWhiteSpace(candidate.UserId))
                {
                    logger.LogCritical("Deletion request {DeletionRequestId} reached ExternalSubscriptionsClosed without a user binding.", candidate.Id);
                    continue;
                }

                var local = await admin.DeleteUserForDeletionWorkflowAsync(candidate.UserId, candidate.Id);
                if (!local.Success)
                {
                    logger.LogWarning("Local deletion recovery remains pending for request {DeletionRequestId}.", candidate.Id);
                    continue;
                }
            }

            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            await DatabaseApplicationLock.AcquireAsync(db,$"InkRoute:AccountDeletion:{candidate.Id:N}","account_deletion_concurrency_conflict","Account deletion is already being finalized.");
            db.ChangeTracker.Clear();
            var request = await db.AccountDeletionRequests.FirstOrDefaultAsync(x => x.Id == candidate.Id, cancellationToken);
            if (request?.State == AccountDeletionState.LocalDeletionCompleted)
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                request.State = AccountDeletionState.Completed;
                request.UserId = null;
                request.LastErrorCode = null;
                request.CompletedAt = now;
                request.UpdatedAt = now;
                db.AccountDeletionAudits.Add(new AccountDeletionAudit
                {
                    DeletedAt = now,
                    RetentionNote = "Deletion audit retained for legal and security accountability; no user identifier is stored in this audit record."
                });
                await db.SaveChangesAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
        }
    }
}
