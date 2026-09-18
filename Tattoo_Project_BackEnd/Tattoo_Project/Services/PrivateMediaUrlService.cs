using Microsoft.AspNetCore.DataProtection;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services;

public sealed class PrivateMediaUrlService(
    IDataProtectionProvider dataProtectionProvider,
    IWebHostEnvironment environment,
    IFileStorage storage) : IPrivateMediaUrlService
{
    private static readonly string[] LegacyPrivatePrefixes =
    [
        "/uploads/tattoo-request-images/",
        "/uploads/ai-tattoos/"
    ];

    private readonly ITimeLimitedDataProtector protector = dataProtectionProvider
        .CreateProtector("InkRoute.PrivateMedia.Read.v2")
        .ToTimeLimitedDataProtector();

    public string CreateReadUrl(string storedPath)
    {
        if (storage.IsManagedKey(storedPath))
        {
            if (storedPath.StartsWith("public/", StringComparison.Ordinal))
                return $"/api/media/public?key={Uri.EscapeDataString(storedPath)}";
            var managedToken = protector.Protect(storedPath, TimeSpan.FromMinutes(10));
            return $"/api/media/private?token={Uri.EscapeDataString(managedToken)}";
        }
        if (!IsLegacyPrivateStoredPath(storedPath)) return storedPath;
        var token = protector.Protect(storedPath, TimeSpan.FromMinutes(10));
        return $"/api/media/private?token={Uri.EscapeDataString(token)}";
    }

    public bool TryResolveReadToken(string token, out string fullPath, out string contentType)
    {
        fullPath = string.Empty; contentType = string.Empty;
        if (string.IsNullOrWhiteSpace(token)) return false;
        string storedPath;
        try { storedPath = protector.Unprotect(token); }
        catch { return false; }

        if (storage.IsManagedKey(storedPath))
        {
            return storage.TryResolvePath(storedPath, out fullPath, out contentType, out var visibility) &&
                   visibility == StoredFileVisibility.Private;
        }
        return TryResolveLegacyPrivate(storedPath, out fullPath, out contentType);
    }

    public bool TryResolvePublicKey(string key, out string fullPath, out string contentType) =>
        storage.TryResolvePath(key, out fullPath, out contentType, out var visibility) &&
        visibility == StoredFileVisibility.Public;

    public static bool IsPrivateStoredPath(string? path) =>
        (!string.IsNullOrWhiteSpace(path) && path.StartsWith("private/", StringComparison.Ordinal)) ||
        IsLegacyPrivateStoredPath(path);

    private static bool IsLegacyPrivateStoredPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && LegacyPrivatePrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal));

    private bool TryResolveLegacyPrivate(string storedPath, out string fullPath, out string contentType)
    {
        fullPath = string.Empty; contentType = string.Empty;
        if (!IsLegacyPrivateStoredPath(storedPath)) return false;
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var canonicalRoot = Path.GetFullPath(webRoot) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(webRoot, storedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(canonicalRoot, StringComparison.Ordinal)) return false;
        contentType = Path.GetExtension(candidate).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => string.Empty
        };
        if (contentType.Length == 0 || !File.Exists(candidate)) return false;
        fullPath = candidate;
        return true;
    }
}
