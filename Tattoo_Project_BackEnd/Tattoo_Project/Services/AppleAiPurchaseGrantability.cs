using System.Text.Json;

namespace Tattoo_Project.Services;

public enum AppleAiPurchaseGrantability
{
    ValidAndGrantable,
    ValidButRevoked
}

public static class AppleAiPurchaseGrantabilityRules
{
    public static AppleAiPurchaseGrantability Evaluate(JsonElement transaction)
    {
        if (transaction.TryGetProperty("revocationDate", out var revoked) &&
            revoked.ValueKind is JsonValueKind.Number or JsonValueKind.String)
            return AppleAiPurchaseGrantability.ValidButRevoked;

        return AppleAiPurchaseGrantability.ValidAndGrantable;
    }
}
