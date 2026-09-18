namespace Tattoo_Project.Services.Interfaces;

public interface IPrivateMediaUrlService
{
    string CreateReadUrl(string storedPath);
    bool TryResolveReadToken(string token, out string fullPath, out string contentType);
    bool TryResolvePublicKey(string key, out string fullPath, out string contentType);
}
