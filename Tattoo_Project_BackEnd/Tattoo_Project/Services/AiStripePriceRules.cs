namespace Tattoo_Project.Services;

public static class AiStripePriceRules
{
    public const string CheckoutMode = "payment";

    public static bool IsValidOneTimePrice(
        bool active,
        bool hasRecurringConfiguration,
        long? unitAmount,
        string? actualCurrency,
        string? taxBehavior,
        long expectedAmount,
        string expectedCurrency) =>
        active &&
        !hasRecurringConfiguration &&
        unitAmount == expectedAmount &&
        string.Equals(actualCurrency, expectedCurrency, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(taxBehavior, "exclusive", StringComparison.OrdinalIgnoreCase);
}
