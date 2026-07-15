namespace Tattoo_Project.AI.Planning;

public interface IAiTattooPlanner
{
    Task<string> CreateGenerationPromptAsync(
        string planningContext,
        CancellationToken cancellationToken = default);

    Task<string> CreateEditPromptAsync(
        string planningContext,
        CancellationToken cancellationToken = default);
}
