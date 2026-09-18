namespace Tattoo_Project.Models;

public static class AiProjectCheckoutAttemptStatuses
{
    public const string Creating = "Creating";
    public const string SessionCreated = "SessionCreated";
    public const string PaymentConfirmed = "PaymentConfirmed";
    public const string GrantPending = "GrantPending";
    public const string Granted = "Granted";
    public const string Failed = "Failed";
    public const string Expired = "Expired";

    public static bool IsActive(string status) => status is Creating or SessionCreated or PaymentConfirmed or GrantPending;
}

public class AiProjectCheckoutAttempt
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public int AiTattooProjectId { get; set; }
    public AiTattooProject AiTattooProject { get; set; } = null!;
    public string Product { get; set; } = "ai_project_pass";
    public long ExpectedBaseAmountMinor { get; set; }
    public string Currency { get; set; } = "eur";
    public string PriceSemantics { get; set; } = "pre_tax";
    public string Status { get; set; } = AiProjectCheckoutAttemptStatuses.Creating;
    public string StripeIdempotencyKey { get; set; } = null!;
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripeCheckoutUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? FailureCode { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public static class AiProjectCheckoutAttemptRules
{
    public static bool CanTransition(string current, string next) => current == next || (current, next) switch
    {
        (AiProjectCheckoutAttemptStatuses.Creating, AiProjectCheckoutAttemptStatuses.SessionCreated) => true,
        (AiProjectCheckoutAttemptStatuses.Creating, AiProjectCheckoutAttemptStatuses.Failed) => true,
        (AiProjectCheckoutAttemptStatuses.SessionCreated, AiProjectCheckoutAttemptStatuses.PaymentConfirmed) => true,
        (AiProjectCheckoutAttemptStatuses.SessionCreated, AiProjectCheckoutAttemptStatuses.Expired) => true,
        (AiProjectCheckoutAttemptStatuses.SessionCreated, AiProjectCheckoutAttemptStatuses.Failed) => true,
        (AiProjectCheckoutAttemptStatuses.PaymentConfirmed, AiProjectCheckoutAttemptStatuses.GrantPending) => true,
        (AiProjectCheckoutAttemptStatuses.GrantPending, AiProjectCheckoutAttemptStatuses.Granted) => true,
        _ => false
    };

    public static void Transition(AiProjectCheckoutAttempt attempt, string next, DateTime now)
    {
        if (!CanTransition(attempt.Status, next))
            throw new InvalidOperationException($"Invalid AI checkout transition {attempt.Status} -> {next}.");
        attempt.Status = next;
        attempt.UpdatedAtUtc = now;
        if (next is AiProjectCheckoutAttemptStatuses.Granted or AiProjectCheckoutAttemptStatuses.Failed or AiProjectCheckoutAttemptStatuses.Expired)
            attempt.CompletedAtUtc = now;
    }
}
