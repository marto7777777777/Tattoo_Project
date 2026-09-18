using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public class ProviderWebhookEventService(
    TattooDbContext db,
    IPurchasePayloadProtector protector,
    TimeProvider timeProvider,
    ILogger<ProviderWebhookEventService> logger) : IProviderWebhookEventService
{
    public async Task<ResultService> ExecuteOnceAsync(
        string provider,
        string eventId,
        string eventType,
        string payload,
        Func<Task<ResultService>> handler)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = protector.Hash(payload);
        var row = await db.ProviderWebhookEvents
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ExternalEventId == eventId);

        if (row?.ProcessingStatus == "processed")
        {
            // Legacy Stripe rows were migrated before raw payload hashes were stored.
            var legacyStripeHash = provider == SubscriptionProviders.Stripe &&
                                   row.PayloadHash == protector.Hash(row.ExternalEventId);
            return row.PayloadHash == hash || legacyStripeHash
                ? ResultService.Ok()
                : ResultService.Fail("Event payload hash mismatch.");
        }

        if (row?.ProcessingStatus == "processing" && row.ReceivedAt > now.AddMinutes(-10))
            return ResultService.Fail("Provider event is already processing; retry later.");

        if (row == null)
        {
            row = new ProviderWebhookEvent
            {
                Provider = provider,
                ExternalEventId = eventId,
                EventType = eventType,
                PayloadHash = hash,
                ReceivedAt = now
            };
            db.ProviderWebhookEvents.Add(row);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogInformation(ex, "Provider webhook duplicate/race detected for {Provider}/{EventType}.", provider, eventType);
                return ResultService.Fail("Provider event is already processing; retry later.");
            }
        }
        else
        {
            if (row.PayloadHash != hash)
                return ResultService.Fail("Event payload hash mismatch.");

            row.ProcessingStatus = "processing";
            row.Error = null;
            row.ReceivedAt = now;
            await db.SaveChangesAsync();
        }

        try
        {
            var result = await handler();
            if (!result.Success)
                throw new InvalidOperationException(result.ErrorMessage ?? "Provider handler returned failure.");

            row.ProcessingStatus = "processed";
            row.ProcessedAt = timeProvider.GetUtcNow().UtcDateTime;
            row.Error = null;
            await db.SaveChangesAsync();
            return ResultService.Ok();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Provider webhook processing failed for {Provider}/{EventType}; event remains retryable.", provider, eventType);
            row.ProcessingStatus = "failed";
            row.Error = "handler_failed";
            await db.SaveChangesAsync();
            return ResultService.Fail("Provider event processing failed.");
        }
    }
}
