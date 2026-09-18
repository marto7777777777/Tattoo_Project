using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.Models;

namespace Tattoo_Project.Services;

/// <summary>
/// Recovers AI operations left in Generating after a process/container crash.
/// Lease expiry alone is not sufficient: the project fencing identity must still match.
/// </summary>
public sealed class AiGenerationRecoveryHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<AiGenerationRecoveryHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        do
        {
            try { await RecoverAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "AI generation recovery failed; it will retry on the next interval."); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task RecoverAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TattooDbContext>();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var candidates = await db.AiGenerationOperations.AsNoTracking()
            .Where(x => x.Status == AiGenerationOperationStatuses.Generating && x.LeaseExpiresAtUtc <= now)
            .OrderBy(x => x.LeaseExpiresAtUtc).Select(x => new { x.OperationId, x.AiTattooProjectId, x.OperationEpoch }).Take(50)
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            try
            {
                await DatabaseApplicationLock.AcquireAsync(db,$"InkRoute:AiProject:{candidate.AiTattooProjectId}","ai_operation_concurrency_conflict","The AI project is being recovered by another request.");
                db.ChangeTracker.Clear();
                var operation = await db.AiGenerationOperations.FirstOrDefaultAsync(x => x.OperationId == candidate.OperationId, cancellationToken);
                var project = await db.AiTattooProjects.FirstOrDefaultAsync(x => x.Id == candidate.AiTattooProjectId, cancellationToken);
                if (operation != null && project != null && operation.Status == AiGenerationOperationStatuses.Generating &&
                    operation.LeaseExpiresAtUtc <= now && AiGenerationRules.OwnsFence(project, operation.OperationId, operation.OperationEpoch))
                {
                    operation.Status = AiGenerationOperationStatuses.Abandoned;
                    operation.FailureCode = "lease_expired";
                    operation.CompletedAtUtc = now;
                    operation.UpdatedAt = now;
                    project.ActiveOperationId = null;
                    project.ConsecutiveGenerationFailures = Math.Min(AiGenerationRules.MaxConsecutiveFailures, project.ConsecutiveGenerationFailures + 1);
                    project.UpdatedAt = now;
                    await db.SaveChangesAsync(cancellationToken);
                }
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
