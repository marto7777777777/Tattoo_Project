namespace Tattoo_Project.Models;

public static class AiPurchaseStates
{
    public const string Received = "Received";
    public const string Verified = "Verified";
    public const string ConsumptionPending = "ConsumptionPending";
    public const string Consumed = "Consumed";
    public const string GrantPending = "GrantPending";
    public const string Granted = "Granted";
    public const string Retryable = "Retryable";
    public const string SandboxVerified = "SandboxVerified";
    public const string Revoked = "Revoked";
    public const string Invalid = "Invalid";
}

public static class AiPurchaseStateRules
{
    private static readonly Dictionary<string, int> Rank = new(StringComparer.Ordinal)
    {
        [AiPurchaseStates.Received] = 0,
        [AiPurchaseStates.Verified] = 10,
        [AiPurchaseStates.ConsumptionPending] = 20,
        [AiPurchaseStates.Consumed] = 30,
        [AiPurchaseStates.GrantPending] = 40,
        [AiPurchaseStates.Retryable] = 45,
        [AiPurchaseStates.Granted] = 100,
        [AiPurchaseStates.SandboxVerified] = 100,
        [AiPurchaseStates.Revoked] = 100,
        [AiPurchaseStates.Invalid] = 100
    };

    public static bool IsTerminal(string state) =>
        state is AiPurchaseStates.Granted or AiPurchaseStates.SandboxVerified or AiPurchaseStates.Revoked or AiPurchaseStates.Invalid;

    public static bool BlocksDraftCleanup(string state) =>
        state is not (AiPurchaseStates.Invalid or AiPurchaseStates.Revoked or AiPurchaseStates.SandboxVerified);

    public static bool CanTransition(string current, string next)
    {
        if (string.Equals(current, next, StringComparison.Ordinal)) return true;
        if (IsTerminal(current)) return false;
        if (next is AiPurchaseStates.Revoked or AiPurchaseStates.Invalid or AiPurchaseStates.SandboxVerified) return true;
        return Rank.TryGetValue(current, out var currentRank) && Rank.TryGetValue(next, out var nextRank) && nextRank >= currentRank;
    }
}
