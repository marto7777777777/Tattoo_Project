using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services;

public sealed class FileCleanupHostedService(IServiceScopeFactory scopeFactory, ILogger<FileCleanupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Post-commit file cleanup worker failed."); }
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TattooDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var tasks = await db.FileCleanupTasks.Where(x => x.CompletedAt == null && x.RetryCount < 20).OrderBy(x => x.UpdatedAt).Take(25).ToListAsync(cancellationToken);
        foreach (var item in tasks)
        {
            try
            {
                await storage.DeleteAsync(item.StorageKey, cancellationToken);
                item.CompletedAt = DateTime.UtcNow;
                item.UpdatedAt = item.CompletedAt.Value;
                item.LastError = null;
            }
            catch (Exception ex)
            {
                item.RetryCount++;
                item.UpdatedAt = DateTime.UtcNow;
                item.LastError = ex.GetType().Name;
                logger.LogWarning(ex, "Deferred file cleanup failed for cleanup task {CleanupTaskId}.", item.Id);
            }
        }
        if (tasks.Count > 0) await db.SaveChangesAsync(cancellationToken);
    }
}
