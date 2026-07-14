namespace Tattoo_Project.AI.Models;

public sealed class AiTattooEditContext
{
    public string TattooStyle { get; init; } = string.Empty;
    public string Placement { get; init; } = string.Empty;
    public string InitialDescription { get; init; } = string.Empty;
    public string EditInstruction { get; init; } = string.Empty;
}
