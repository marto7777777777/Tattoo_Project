namespace Tattoo_Project.AI.Models;

public sealed class AiTattooPromptContext
{
    public string TattooStyle { get; init; } = string.Empty;
    public string Placement { get; init; } = string.Empty;
    public string ClientDescription { get; init; } = string.Empty;
    public bool HasReferenceImage { get; init; }
}
