namespace Tattoo_Project.DTOs.SubscriptionDTOs;
public class VerifiedProviderSubscription
{
 public string Provider { get; set; }=null!;public string ExternalSubscriptionId { get; set; }=null!;public string? ExternalTransactionId { get; set; }public string ProductId { get; set; }=null!;public string Status { get; set; }=null!;public DateTime? TrialStartsAt { get; set; }public DateTime? TrialEndsAt { get; set; }public DateTime? CurrentPeriodStartsAt { get; set; }public DateTime? CurrentPeriodEndsAt { get; set; }public bool CancelAtPeriodEnd { get; set; }public bool IsSandbox { get; set; }public bool IsTrial { get; set; }public string? RawPurchasePayload { get; set; } public DateTime? ProviderEventAt { get; set; } public string? ProviderEventId { get; set; }
}
