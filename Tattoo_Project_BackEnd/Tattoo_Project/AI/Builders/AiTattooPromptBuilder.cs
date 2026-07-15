using System.Text.RegularExpressions;
using Tattoo_Project.AI.Models;
using Tattoo_Project.AI.Providers;

namespace Tattoo_Project.AI.Builders;

public sealed class AiTattooPromptBuilder(
    IPromptFileProvider promptFiles) : IAiTattooPromptBuilder
{
    public async Task<string> BuildGenerationPromptAsync(
        AiTattooPromptContext context,
        CancellationToken cancellationToken = default)
    {
        Validate(
            context.TattooStyle,
            context.Placement,
            context.ClientDescription);

        var template = await promptFiles.ReadRequiredAsync(
            "Generation/GenerationTemplate.txt",
            cancellationToken);

        var coreRules = await ReadCoreAsync(cancellationToken);

        var styleRules = await ReadMappedRulesAsync(
            "Styles",
            context.TattooStyle,
            cancellationToken);

        var placementRules = await ReadMappedRulesAsync(
            "Placements",
            context.Placement,
            cancellationToken);

        var anatomyRules = await promptFiles.ReadRequiredAsync(
            "Anatomy/AnatomyRules.txt",
            cancellationToken);

        var professionalCompositionRules =
            await promptFiles.ReadRequiredAsync(
                "Composition/CompositionRules.txt",
                cancellationToken);

        return ReplaceTokens(
            template,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["{{CORE_RULES}}"] = coreRules,

                ["{{TATTOO_STYLE}}"] =
                    context.TattooStyle.Trim(),

                ["{{STYLE_RULES}}"] =
                    styleRules,

                ["{{BODY_PLACEMENT}}"] =
                    context.Placement.Trim(),

                ["{{PLACEMENT_RULES}}"] =
                    placementRules,

                ["{{ANATOMY_RULES}}"] =
                    anatomyRules,

                ["{{PROFESSIONAL_COMPOSITION_RULES}}"] =
                    professionalCompositionRules,

                ["{{CLIENT_DESCRIPTION}}"] =
                    context.ClientDescription.Trim(),

                ["{{REFERENCE_INSTRUCTIONS}}"] =
                    context.HasReferenceImage
                        ? """
                          A reference image is supplied.

                          Use the reference image only as visual guidance.

                          Preserve only the characteristics that are relevant
                          to the client's written request.

                          Do not copy the image exactly.

                          Create an original tattoo composition.

                          The written client description, locked tattoo style
                          and locked body placement have priority over
                          accidental details in the reference image.
                          """
                        : """
                          No reference image is supplied.

                          Build the tattoo concept from the client's written
                          description, selected tattoo style, selected body
                          placement and professional tattoo-design rules.
                          """
            });
    }

    public async Task<string> BuildEditPromptAsync(
        AiTattooEditContext context,
        CancellationToken cancellationToken = default)
    {
        Validate(
            context.TattooStyle,
            context.Placement,
            context.InitialDescription);

        if (string.IsNullOrWhiteSpace(context.EditInstruction))
        {
            throw new ArgumentException(
                "Edit instruction is required.",
                nameof(context));
        }

        var template = await promptFiles.ReadRequiredAsync(
            "Editing/EditTemplate.txt",
            cancellationToken);

        var coreRules = await ReadCoreAsync(cancellationToken);

        var styleRules = await ReadMappedRulesAsync(
            "Styles",
            context.TattooStyle,
            cancellationToken);

        var placementRules = await ReadMappedRulesAsync(
            "Placements",
            context.Placement,
            cancellationToken);

        var anatomyRules = await promptFiles.ReadRequiredAsync(
            "Anatomy/AnatomyRules.txt",
            cancellationToken);

        var professionalCompositionRules =
            await promptFiles.ReadRequiredAsync(
                "Composition/CompositionRules.txt",
                cancellationToken);

        var operationRules =
            await DetectEditOperationRulesAsync(
                context.EditInstruction,
                cancellationToken);

        return ReplaceTokens(
            template,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["{{CORE_RULES}}"] =
                    coreRules,

                ["{{TATTOO_STYLE}}"] =
                    context.TattooStyle.Trim(),

                ["{{STYLE_RULES}}"] =
                    styleRules,

                ["{{BODY_PLACEMENT}}"] =
                    context.Placement.Trim(),

                ["{{PLACEMENT_RULES}}"] =
                    placementRules,

                ["{{ANATOMY_RULES}}"] =
                    anatomyRules,

                ["{{PROFESSIONAL_COMPOSITION_RULES}}"] =
                    professionalCompositionRules,

                ["{{INITIAL_DESCRIPTION}}"] =
                    context.InitialDescription.Trim(),

                ["{{EDIT_REQUEST}}"] =
                    context.EditInstruction.Trim(),

                ["{{EDIT_OPERATION_RULES}}"] =
                    operationRules
            });
    }

    private async Task<string> ReadCoreAsync(
        CancellationToken cancellationToken)
    {
        string[] files =
        [
            "Core/Role.txt",
            "Core/OutputRules.txt",
            "Core/Exclusions.txt",
            "Core/CompositionRules.txt",
            "Core/TattooQualityRules.txt",
            "Core/MasterTattooDesignRules.txt",
            "Core/FinalChecklist.txt"
        ];

        var sections = new List<string>(files.Length);

        foreach (var file in files)
        {
            var section = await promptFiles.ReadRequiredAsync(
                file,
                cancellationToken);

            sections.Add(section);
        }

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            sections);
    }

    private async Task<string> ReadMappedRulesAsync(
        string folder,
        string userValue,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeFileName(userValue);
        var requestedPath = $"{folder}/{normalizedName}.txt";

        var rules = await promptFiles.ReadOptionalAsync(
            requestedPath,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(rules))
        {
            return rules;
        }

        return await promptFiles.ReadRequiredAsync(
            $"{folder}/Default.txt",
            cancellationToken);
    }

    private async Task<string> DetectEditOperationRulesAsync(
        string instruction,
        CancellationToken cancellationToken)
    {
        var normalizedInstruction =
            instruction.Trim().ToLowerInvariant();

        var files = new List<string>();

        if (ContainsAny(
                normalizedInstruction,
                "add",
                "include",
                "insert",
                "place",
                "добав",
                "сложи",
                "включи"))
        {
            files.Add("Editing/AddInstruction.txt");
        }

        if (ContainsAny(
                normalizedInstruction,
                "remove",
                "delete",
                "without",
                "erase",
                "мах",
                "премах",
                "изтрий",
                "без "))
        {
            files.Add("Editing/RemoveInstruction.txt");
        }

        if (ContainsAny(
                normalizedInstruction,
                "larger",
                "bigger",
                "increase",
                "smaller",
                "decrease",
                "resize",
                "увелич",
                "по-голям",
                "по голям",
                "намал",
                "по-малък",
                "по малък"))
        {
            files.Add("Editing/ResizeInstruction.txt");
        }

        if (ContainsAny(
                normalizedInstruction,
                "color",
                "colour",
                "red",
                "blue",
                "green",
                "yellow",
                "orange",
                "purple",
                "black",
                "grey",
                "gray",
                "цвет",
                "черв",
                "син",
                "зелен",
                "жълт",
                "оранжев",
                "лилав",
                "черен",
                "сив"))
        {
            files.Add("Editing/ColorInstruction.txt");
        }

        if (ContainsAny(
                normalizedInstruction,
                "simplify",
                "minimal",
                "cleaner",
                "less detail",
                "reduce detail",
                "опрост",
                "минимал",
                "по-чист",
                "по чист",
                "по-малко детайли",
                "по малко детайли"))
        {
            files.Add("Editing/SimplifyInstruction.txt");
        }

        if (files.Count == 0)
        {
            return """
                   Apply only the requested refinement.

                   Preserve all existing approved content that the client
                   did not explicitly request to change.

                   Do not restart the design from the beginning.

                   Do not produce an unrelated alternative.

                   Maintain the locked tattoo style, locked body placement,
                   composition, subject identity, anatomy, visual hierarchy
                   and tattoo readability.
                   """;
        }

        var sections = new List<string>();

        foreach (var file in files.Distinct(
                     StringComparer.OrdinalIgnoreCase))
        {
            var section = await promptFiles.ReadRequiredAsync(
                file,
                cancellationToken);

            sections.Add(section);
        }

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            sections);
    }

    private static string ReplaceTokens(
        string template,
        IReadOnlyDictionary<string, string> values)
    {
        var output = template;

        foreach (var pair in values)
        {
            output = output.Replace(
                pair.Key,
                pair.Value,
                StringComparison.Ordinal);
        }

        var unresolvedTokens = Regex
            .Matches(output, @"\{\{[A-Z0-9_]+\}\}")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (unresolvedTokens.Length > 0)
        {
            throw new InvalidOperationException(
                $"Unresolved AI prompt tokens: " +
                string.Join(", ", unresolvedTokens));
        }

        return output.Trim();
    }

    private static string NormalizeFileName(string value)
    {
        return Regex.Replace(
            value.Trim(),
            "[^A-Za-z0-9]",
            string.Empty);
    }

    private static bool ContainsAny(
        string value,
        params string[] terms)
    {
        return terms.Any(
            term => value.Contains(
                term,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void Validate(
        string style,
        string placement,
        string description)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            throw new ArgumentException(
                "Tattoo style is required.");
        }

        if (string.IsNullOrWhiteSpace(placement))
        {
            throw new ArgumentException(
                "Body placement is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Client description is required.");
        }
    }
}