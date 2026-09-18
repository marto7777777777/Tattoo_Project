namespace Tattoo_Project.Models;

public class ProviderSubscription
{
    public long Id { get; set; }
    public int ArtistSubscriptionId { get; set; }
    public ArtistSubscription ArtistSubscription { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ExternalSubscriptionId { get; set; } = null!;
    public string? ExternalTransactionId { get; set; }
    public string ProductId { get; set; } = null!;
    public string Status { get; set; } = ArtistSubscriptionStatuses.Pending;
    public DateTime? TrialStartsAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CurrentPeriodStartsAt { get; set; }
    public DateTime? CurrentPeriodEndsAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public bool IsSandbox { get; set; }
    public string? PurchaseTokenHash { get; set; }
    public string? EncryptedPurchasePayload { get; set; }
    public DateTime FirstVerifiedAt { get; set; }
    public DateTime LastVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastProviderEventAt { get; set; }
    public string? LastProviderEventId { get; set; }
}
