namespace Tattoo_Project.DTOs.SubscriptionDTOs;

public class SubscriptionStatusDto
{
    public string Status { get; set; } = "not_applicable";
    public bool HasAccess { get; set; }
    public bool RequiresCheckout { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CurrentPeriodEndsAt { get; set; }
}

public class StripeRedirectDto { public string Url { get; set; } = null!; }
