namespace Tattoo_Project.Models;

public class PendingEmailChange
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public string NewEmail { get; set; } = null!;
    public string NormalizedNewEmail { get; set; } = null!;
    public string CodeHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? UsedAt { get; set; }
}
