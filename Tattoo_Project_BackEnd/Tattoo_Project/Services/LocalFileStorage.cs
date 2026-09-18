using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services;

public sealed class LocalFileStorage(IConfiguration configuration, IWebHostEnvironment environment, ILogger<LocalFileStorage> logger) : IFileStorage
{
    private readonly string root = ResolveRoot(configuration, environment);
    private readonly string webRoot = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".webp", ".png", ".jpg", ".jpeg" };

    public async Task<string> SaveAsync(ReadOnlyMemory<byte> content, string category, string extension, StoredFileVisibility visibility, CancellationToken cancellationToken = default)
    {
        extension = extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension)) throw new InvalidOperationException("Unsupported managed-media extension.");
        var safeCategory = new string(category.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').ToArray());
        if (string.IsNullOrWhiteSpace(safeCategory)) throw new InvalidOperationException("Invalid storage category.");
        var prefix = visibility == StoredFileVisibility.Private ? "private" : "public";
        var key = $"{prefix}/{safeCategory}/{Guid.NewGuid():N}{extension}";
        var path = ResolveManagedPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content.ToArray(), cancellationToken);
        return key;
    }

    public Task<bool> DeleteAsync(string? storageKey, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<bool>(cancellationToken);

        string? path = null;
        if (TryResolvePath(storageKey, out var managedPath, out _, out _))
            path = managedPath;
        else if (TryResolveLegacyUploadPath(storageKey, out var legacyPath))
            path = legacyPath;

        if (path == null) return Task.FromResult(false);
        try
        {
            if (!File.Exists(path)) return Task.FromResult(false);
            File.Delete(path);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Media cleanup failed for storage key {StorageKey}.", storageKey);
            throw;
        }
    }

    public bool TryResolvePath(string? storageKey, out string fullPath, out string contentType, out StoredFileVisibility visibility)
    {
        fullPath = string.Empty; contentType = string.Empty; visibility = StoredFileVisibility.Private;
        if (!IsManagedKey(storageKey)) return false;
        try
        {
            var candidate = ResolveManagedPath(storageKey!);
            if (!File.Exists(candidate)) return false;
            contentType = Path.GetExtension(candidate).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => string.Empty
            };
            if (contentType.Length == 0) return false;
            visibility = storageKey!.StartsWith("public/", StringComparison.Ordinal) ? StoredFileVisibility.Public : StoredFileVisibility.Private;
            fullPath = candidate;
            return true;
        }
        catch { return false; }
    }

    public bool IsManagedKey(string? storageKey) =>
        !string.IsNullOrWhiteSpace(storageKey) &&
        (storageKey.StartsWith("public/", StringComparison.Ordinal) || storageKey.StartsWith("private/", StringComparison.Ordinal)) &&
        !storageKey.Contains("..", StringComparison.Ordinal) && !Path.IsPathRooted(storageKey);


    private bool TryResolveLegacyUploadPath(string? storageKey, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(storageKey) ||
            !storageKey.StartsWith("/uploads/", StringComparison.Ordinal) ||
            storageKey.Contains("..", StringComparison.Ordinal))
            return false;

        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"))
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(webRoot, storageKey.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(uploadsRoot, StringComparison.Ordinal)) return false;
        fullPath = candidate;
        return true;
    }

    private string ResolveManagedPath(string key)
    {
        var canonicalRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(canonicalRoot, StringComparison.Ordinal)) throw new InvalidOperationException("Storage path traversal was rejected.");
        return candidate;
    }

    private static string ResolveRoot(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration["Storage:RootPath"];
        var value = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, ".inkroute-storage")
            : configured;
        if (!Path.IsPathRooted(value)) value = Path.GetFullPath(Path.Combine(environment.ContentRootPath, value));
        Directory.CreateDirectory(value);
        return Path.GetFullPath(value);
    }
}
