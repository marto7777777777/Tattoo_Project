namespace Tattoo_Project.Models;

public class AnalyticsOutboxEvent
{
    public long Id { get; set; }
    public int TattooArtistId { get; set; }
    public string Name { get; set; } = null!;
    public int? CompletedProjectCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
}
