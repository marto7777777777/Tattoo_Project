namespace Tattoo_Project.Models;

public class AiProjectStorePurchase
{
    public long Id { get; set; }
    public int? AiTattooProjectId { get; set; }
    public AiTattooProject? AiTattooProject { get; set; }
    public string UserId { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ExternalTransactionId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public string PurchasePayloadHash { get; set; } = null!;
    public string EncryptedPurchasePayload { get; set; } = null!;
    public bool IsSandbox { get; set; }
    public string ProcessingState { get; set; } = "Received";
    public DateTime VerifiedAt { get; set; }
    public DateTime AccessGrantedUntil { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTime? DeadLetteredAtUtc { get; set; }
}
