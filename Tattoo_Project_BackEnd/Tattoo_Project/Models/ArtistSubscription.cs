namespace Tattoo_Project.Models;

public static class ArtistSubscriptionStatuses
{
    public const string Pending = "pending_subscription";
    public const string Trialing = "trialing";
    public const string Active = "active";
    public const string PastDue = "past_due";
    public const string Incomplete = "incomplete";
    public const string Cancelled = "cancelled";
    public const string Ended = "ended";

    public static bool GrantsAccess(string status) => status is Trialing or Active;
}

public class ArtistSubscription
{
    public int Id { get; set; }
    public int TattooArtistId { get; set; }
    public TattooArtist TattooArtist { get; set; } = null!;
    public string Status { get; set; } = ArtistSubscriptionStatuses.Pending;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CurrentPeriodEndsAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public int SuccessfulPaidInvoiceCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
