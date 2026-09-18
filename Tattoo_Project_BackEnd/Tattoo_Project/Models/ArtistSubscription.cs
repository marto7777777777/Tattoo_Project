namespace Tattoo_Project.Models;

public static class ArtistSubscriptionStatuses
{
    public const string Pending = "pending_subscription";
    public const string Trialing = "trialing";
    public const string Active = "active";
    public const string GracePeriod = "grace_period";
    public const string OnHold = "on_hold";
    public const string Paused = "paused";
    public const string PastDue = "past_due";
    public const string Incomplete = "incomplete";
    public const string Cancelled = "cancelled";
    public const string Ended = "ended";
    public const string Expired = "expired";
    public const string Revoked = "revoked";

    public static bool GrantsAccess(string status) => status is Trialing or Active or GracePeriod;
}

public static class SubscriptionProviders
{
    public const string Stripe = "stripe";
    public const string GooglePlay = "google_play";
    public const string Apple = "apple";
}

public class ArtistSubscription
{
    public int Id { get; set; }
    public int TattooArtistId { get; set; }
    public TattooArtist TattooArtist { get; set; } = null!;
    public string Status { get; set; } = ArtistSubscriptionStatuses.Pending;
    public string? ActiveProvider { get; set; }
    public bool HasUsedTrial { get; set; }
    public DateTime? FirstActivatedAt { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public string? GoogleObfuscatedAccountId { get; set; }
    public Guid? AppleAppAccountToken { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CurrentPeriodEndsAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public string? PendingProvider { get; set; }
    public DateTime? PendingProviderExpiresAt { get; set; }
    public Guid? StripeCheckoutAttemptToken { get; set; }
    public int SuccessfulPaidInvoiceCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public ICollection<ProviderSubscription> ProviderSubscriptions { get; set; } = [];
}
