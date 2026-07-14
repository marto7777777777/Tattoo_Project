namespace Tattoo_Project.Models;

public class AiProjectPayment
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public int AiTattooProjectId { get; set; }
    public AiTattooProject AiTattooProject { get; set; } = null!;
    public string StripeCheckoutSessionId { get; set; } = null!;
    public string? StripePaymentIntentId { get; set; }
    public long AmountInMinorUnits { get; set; }
    public string Currency { get; set; } = "eur";
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? AccessGrantedUntil { get; set; }
}
