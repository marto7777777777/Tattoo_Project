namespace Tattoo_Project.Models;

public class ProviderWebhookEvent
{
    public long Id { get; set; }
    public string Provider { get; set; } = null!;
    public string ExternalEventId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string PayloadHash { get; set; } = null!;
    public string ProcessingStatus { get; set; } = "processing";
    public string? Error { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
