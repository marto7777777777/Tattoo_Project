namespace Tattoo_Project.DTOs.SubscriptionDTOs;

public class SubscriptionStatusDto
{
    public string Status { get; set; } = "not_applicable";
    public bool HasAccess { get; set; }
    public bool RequiresCheckout { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CurrentPeriodEndsAt { get; set; }
    public string? ActiveProvider { get; set; }
    public bool HasUsedTrial { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public bool IsSandboxEntitlement { get; set; }
}

public class StripeRedirectDto { public string Url { get; set; } = null!; }
public class MobileBillingContextDto
{
 public string Provider { get; set; }=null!; public string ArtistProductId { get; set; }=null!; public string AiProjectPassProductId { get; set; }=null!; public string? BasePlanId { get; set; } public string? TrialOfferId { get; set; } public bool TrialEligible { get; set; } public string? ObfuscatedAccountId { get; set; } public Guid? AppAccountToken { get; set; }
}
public class MobilePurchaseVerificationDto
{
 public string Provider { get; set; }=null!; public string ProductKind { get; set; }="artist_subscription"; public string ProductId { get; set; }=null!; public string PurchasePayload { get; set; }=null!; public int? AiProjectId { get; set; }
}
public class MobilePurchaseVerificationResultDto { public bool Verified { get; set; } public bool HasAccess { get; set; } public DateTime? AccessUntil { get; set; } public bool IsSandboxEntitlement { get; set; } }
public class AppleNotificationDto { public string SignedPayload { get; set; }=null!; }
public class GooglePubSubPushDto { public GooglePubSubMessage Message { get; set; }=new(); public string? Subscription { get; set; } }
public class GooglePubSubMessage { public string Data { get; set; }=null!; public string MessageId { get; set; }=null!; public Dictionary<string,string>? Attributes { get; set; } }
