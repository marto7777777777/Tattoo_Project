namespace Tattoo_Project.Models;

public class StripeWebhookEvent
{
    public long Id { get; set; }
    public string StripeEventId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string Status { get; set; } = "processing";
    public string? Error { get; set; }
}
