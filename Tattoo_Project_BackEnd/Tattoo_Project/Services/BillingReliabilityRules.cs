namespace Tattoo_Project.Services;

public static class AiPassGrantRules
{
    public static DateTime CalculateEnd(DateTime now, DateTime? currentEnd)
    {
        var start = currentEnd.HasValue && currentEnd.Value > now ? currentEnd.Value : now;
        return start.AddDays(30);
    }
}

public static class ProviderEventOrdering
{
    public static bool ShouldApply(DateTime? currentAt, string? currentId, DateTime? incomingAt, string? incomingId)
    {
        if (!incomingAt.HasValue || !currentAt.HasValue) return true;
        if (incomingAt.Value > currentAt.Value) return true;
        if (incomingAt.Value < currentAt.Value) return false;
        if (string.IsNullOrWhiteSpace(incomingId) || string.IsNullOrWhiteSpace(currentId)) return true;
        return string.CompareOrdinal(incomingId, currentId) > 0;
    }
}

public static class BillingSandboxPolicy
{
    public static bool CanGrant(bool isSandbox, bool allowSandboxEntitlements) => !isSandbox || allowSandboxEntitlements;
}
