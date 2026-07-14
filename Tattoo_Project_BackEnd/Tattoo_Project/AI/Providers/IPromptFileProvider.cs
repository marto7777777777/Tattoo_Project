namespace Tattoo_Project.AI.Providers;

public interface IPromptFileProvider
{
    Task<string> ReadRequiredAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<string?> ReadOptionalAsync(string relativePath, CancellationToken cancellationToken = default);
}
