namespace Tattoo_Project.Services;

internal static class PhoneNumberNormalizer
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var compact = new string(value.Trim()
            .Where(character => !char.IsWhiteSpace(character) && character is not '-' and not '(' and not ')' and not '.')
            .ToArray());

        if (compact.StartsWith("00", StringComparison.Ordinal))
            compact = "+" + compact[2..];

        // Requiring an international number avoids treating e.g. 0888... and
        // +359888... as two different accounts simply because of formatting.
        if (!compact.StartsWith('+')) return false;

        var digits = compact[1..];
        if (digits.Length is < 7 or > 15 || digits[0] == '0' || digits.Any(character => !char.IsDigit(character)))
            return false;

        normalized = "+" + digits;
        return true;
    }
}
