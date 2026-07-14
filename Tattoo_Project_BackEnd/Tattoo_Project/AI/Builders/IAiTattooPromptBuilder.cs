using Tattoo_Project.AI.Models;

namespace Tattoo_Project.AI.Builders;

public interface IAiTattooPromptBuilder
{
    Task<string> BuildGenerationPromptAsync(AiTattooPromptContext context, CancellationToken cancellationToken = default);
    Task<string> BuildEditPromptAsync(AiTattooEditContext context, CancellationToken cancellationToken = default);
}
