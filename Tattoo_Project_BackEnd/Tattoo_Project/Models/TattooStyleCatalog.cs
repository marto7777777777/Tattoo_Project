namespace Tattoo_Project.Models
{
    public static class TattooStyleCatalog
    {
        public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Fine Line", "Realism", "Blackwork", "Japanese / Irezumi",
            "American Traditional", "Neo Traditional", "Watercolor",
            "Lettering / Script", "Geometric", "Tribal / Polynesian",
            "Dotwork", "Illustrative", "Chicano", "New School",
            "Biomechanical", "Trash Polka", "Portrait", "Minimalist"
        };

        public static List<string> Normalize(IEnumerable<string>? styles)
            => (styles ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(Allowed.Contains)
                .Take(8)
                .ToList();
    }
}
