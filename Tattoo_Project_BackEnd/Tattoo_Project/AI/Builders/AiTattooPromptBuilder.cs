using System.Text;
using System.Text.RegularExpressions;
using Tattoo_Project.AI.Models;
using Tattoo_Project.AI.Providers;

namespace Tattoo_Project.AI.Builders;

public sealed class AiTattooPromptBuilder(IPromptFileProvider promptFiles) : IAiTattooPromptBuilder
{
    public async Task<string> BuildGenerationPromptAsync(AiTattooPromptContext context, CancellationToken cancellationToken = default)
    {
        Validate(context.TattooStyle, context.Placement, context.ClientDescription);

        var template = await promptFiles.ReadRequiredAsync("Generation/GenerationTemplate.txt", cancellationToken);
        var styleRules = await ReadMappedRulesAsync("Styles", context.TattooStyle, cancellationToken);
        var placementRules = await ReadMappedRulesAsync("Placements", context.Placement, cancellationToken);
        var core = await ReadCoreAsync(cancellationToken);

        return ReplaceTokens(template, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{CORE_RULES}}"] = core,
            ["{{TATTOO_STYLE}}"] = context.TattooStyle.Trim(),
            ["{{STYLE_RULES}}"] = styleRules,
            ["{{BODY_PLACEMENT}}"] = context.Placement.Trim(),
            ["{{PLACEMENT_RULES}}"] = placementRules,
            ["{{CLIENT_DESCRIPTION}}"] = context.ClientDescription.Trim(),
            ["{{REFERENCE_INSTRUCTIONS}}"] = context.HasReferenceImage
                ? "A reference image is supplied. Use it only as visual guidance. Create an original tattoo composition and obey the written client description and locked project constraints."
                : "No reference image is supplied. Build the tattoo concept from the written description and locked project constraints."
        });
    }

    public async Task<string> BuildEditPromptAsync(AiTattooEditContext context, CancellationToken cancellationToken = default)
    {
        Validate(context.TattooStyle, context.Placement, context.InitialDescription);
        if (string.IsNullOrWhiteSpace(context.EditInstruction))
            throw new ArgumentException("Edit instruction is required.", nameof(context));

        var template = await promptFiles.ReadRequiredAsync("Editing/EditTemplate.txt", cancellationToken);
        var styleRules = await ReadMappedRulesAsync("Styles", context.TattooStyle, cancellationToken);
        var placementRules = await ReadMappedRulesAsync("Placements", context.Placement, cancellationToken);
        var core = await ReadCoreAsync(cancellationToken);
        var operationRules = await DetectEditOperationRulesAsync(context.EditInstruction, cancellationToken);

        return ReplaceTokens(template, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{CORE_RULES}}"] = core,
            ["{{TATTOO_STYLE}}"] = context.TattooStyle.Trim(),
            ["{{STYLE_RULES}}"] = styleRules,
            ["{{BODY_PLACEMENT}}"] = context.Placement.Trim(),
            ["{{PLACEMENT_RULES}}"] = placementRules,
            ["{{INITIAL_DESCRIPTION}}"] = context.InitialDescription.Trim(),
            ["{{EDIT_REQUEST}}"] = context.EditInstruction.Trim(),
            ["{{EDIT_OPERATION_RULES}}"] = operationRules
        });
    }

    private async Task<string> ReadCoreAsync(CancellationToken cancellationToken)
    {
        string[] files =
        [
            "Core/Role.txt",
            "Core/OutputRules.txt",
            "Core/Exclusions.txt",
            "Core/CompositionRules.txt",
            "Core/TattooQualityRules.txt",
            "Core/FinalChecklist.txt"
        ];

        var sections = new List<string>(files.Length);
        foreach (var file in files)
            sections.Add(await promptFiles.ReadRequiredAsync(file, cancellationToken));
        return string.Join("\n\n", sections);
    }

    private async Task<string> ReadMappedRulesAsync(string folder, string userValue, CancellationToken cancellationToken)
    {
        var fileName = NormalizeFileName(userValue) + ".txt";
        return await promptFiles.ReadOptionalAsync($"{folder}/{fileName}", cancellationToken)
            ?? await promptFiles.ReadRequiredAsync($"{folder}/Default.txt", cancellationToken);
    }

    private async Task<string> DetectEditOperationRulesAsync(string instruction, CancellationToken cancellationToken)
    {
        var value = instruction.ToLowerInvariant();
        var files = new List<string>();
        if (ContainsAny(value, "add", "include", "insert", "добав")) files.Add("Editing/AddInstruction.txt");
        if (ContainsAny(value, "remove", "delete", "without", "мах", "премах")) files.Add("Editing/RemoveInstruction.txt");
        if (ContainsAny(value, "larger", "bigger", "smaller", "resize", "увелич", "намал")) files.Add("Editing/ResizeInstruction.txt");
        if (ContainsAny(value, "color", "colour", "red", "blue", "green", "black", "цвет", "черв", "син")) files.Add("Editing/ColorInstruction.txt");
        if (ContainsAny(value, "simplify", "minimal", "cleaner", "опрост", "минимал")) files.Add("Editing/SimplifyInstruction.txt");
        if (files.Count == 0) return "Apply only the requested refinement. Preserve all unrelated approved content.";

        var sections = new List<string>();
        foreach (var file in files.Distinct(StringComparer.OrdinalIgnoreCase))
            sections.Add(await promptFiles.ReadRequiredAsync(file, cancellationToken));
        return string.Join("\n", sections);
    }

    private static string ReplaceTokens(string template, IReadOnlyDictionary<string, string> values)
    {
        var output = template;
        foreach (var pair in values) output = output.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
        var unresolved = Regex.Matches(output, @"\{\{[A-Z0-9_]+\}\}").Select(x => x.Value).Distinct().ToArray();
        if (unresolved.Length > 0)
            throw new InvalidOperationException($"Unresolved AI prompt tokens: {string.Join(", ", unresolved)}");
        return output.Trim();
    }

    private static string NormalizeFileName(string value) => Regex.Replace(value, "[^A-Za-z0-9]", string.Empty);
    private static bool ContainsAny(string value, params string[] terms) => terms.Any(value.Contains);
    private static void Validate(string style, string placement, string description)
    {
        if (string.IsNullOrWhiteSpace(style)) throw new ArgumentException("Tattoo style is required.");
        if (string.IsNullOrWhiteSpace(placement)) throw new ArgumentException("Body placement is required.");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Client description is required.");
    }
}
