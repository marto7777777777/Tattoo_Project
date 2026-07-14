using System.Collections.Concurrent;

namespace Tattoo_Project.AI.Providers;

public sealed class PromptFileProvider(IWebHostEnvironment environment, ILogger<PromptFileProvider> logger) : IPromptFileProvider
{
    private readonly ConcurrentDictionary<string, string> cache = new(StringComparer.OrdinalIgnoreCase);
    private string RootPath => Path.Combine(environment.ContentRootPath, "AI", "Prompts");

    public async Task<string> ReadRequiredAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var value = await ReadOptionalAsync(relativePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
            throw new FileNotFoundException($"Required AI prompt file was not found or was empty: {relativePath}");
        return value;
    }

    public async Task<string?> ReadOptionalAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(RootPath, normalized));
        var allowedRoot = Path.GetFullPath(RootPath) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Prompt path escapes the configured prompt directory.");

        if (environment.IsDevelopment())
        {
            if (!File.Exists(fullPath)) return null;
            return (await File.ReadAllTextAsync(fullPath, cancellationToken)).Trim();
        }

        if (cache.TryGetValue(fullPath, out var cached)) return cached;
        if (!File.Exists(fullPath))
        {
            logger.LogWarning("Optional AI prompt file was not found: {PromptPath}", fullPath);
            return null;
        }

        var text = (await File.ReadAllTextAsync(fullPath, cancellationToken)).Trim();
        cache[fullPath] = text;
        return text;
    }
}
