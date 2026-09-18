namespace Tattoo_Project.Models;

public class FileCleanupTask
{
    public long Id { get; set; }
    public string StorageKey { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}
