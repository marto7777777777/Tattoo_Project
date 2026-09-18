using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.Models;

namespace Tattoo_Project.Services;

/// <summary>
/// Removes abandoned unpaid AI drafts. Financially unresolved purchases always block cleanup.
/// Cleanup re-checks authoritative state under the same per-project lock used by payment grant.
/// </summary>
public sealed class AiDraftCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<AiDraftCleanupHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan AbandonAfter = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        do
        {
            try { await CleanupAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "AI draft cleanup failed; it will retry on the next interval."); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TattooDbContext>();
        var cutoff = timeProvider.GetUtcNow().UtcDateTime.Subtract(AbandonAfter);

        // Broad candidate query only. Financial safety is intentionally evaluated after acquiring
        // the same AiPass lock used by purchase binding/grant, so cleanup cannot race a payment.
        var candidateIds = await db.AiTattooProjects.AsNoTracking()
            .Where(project => !project.IsFreeProject && project.CreatedAt < cutoff && project.UpdatedAt < cutoff && project.Versions.Count == 0)
            .OrderBy(project => project.Id).Select(project => project.Id).Take(100)
            .ToListAsync(cancellationToken);

        var removed = 0;
        foreach (var projectId in candidateIds)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            try
            {
                await DatabaseApplicationLock.AcquireAsync(db,$"InkRoute:AiPass:{projectId}","ai_pass_concurrency_conflict","The AI Project Pass is being updated by another request.");
                db.ChangeTracker.Clear();
                var project = await db.AiTattooProjects
                    .Include(x => x.Payments)
                    .Include(x => x.Versions)
                    .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);
                if (project == null) { await transaction.CommitAsync(cancellationToken); continue; }

                var purchaseStates = await db.AiProjectStorePurchases.AsNoTracking()
                    .Where(x => x.AiTattooProjectId == projectId)
                    .Select(x => x.ProcessingState)
                    .ToListAsync(cancellationToken);

                var safeToDelete = !project.IsFreeProject &&
                    project.CreatedAt < cutoff &&
                    project.UpdatedAt < cutoff &&
                    project.Versions.Count == 0 &&
                    (project.EditingAccessUntil == null || project.EditingAccessUntil <= cutoff) &&
                    !project.Payments.Any(payment => payment.Status == "paid") &&
                    !purchaseStates.Any(AiPurchaseStateRules.BlocksDraftCleanup);

                if (!safeToDelete) { await transaction.CommitAsync(cancellationToken); continue; }

                if (!string.IsNullOrWhiteSpace(project.InitialReferenceImageUrl))
                {
                    var now = timeProvider.GetUtcNow().UtcDateTime;
                    db.FileCleanupTasks.Add(new FileCleanupTask
                    {
                        StorageKey = project.InitialReferenceImageUrl,
                        Reason = "ai_draft_cleanup",
                        RetryCount = 0,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                db.AiTattooProjects.Remove(project);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                removed++;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        if (removed > 0) logger.LogInformation("Removed {Count} abandoned unpaid AI drafts.", removed);
    }
}
