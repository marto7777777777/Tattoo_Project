namespace Tattoo_Project.Services.Interfaces;

public enum StoredFileVisibility { Public, Private }

public interface IFileStorage
{
    Task<string> SaveAsync(ReadOnlyMemory<byte> content, string category, string extension, StoredFileVisibility visibility, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string? storageKey, CancellationToken cancellationToken = default);
    bool TryResolvePath(string? storageKey, out string fullPath, out string contentType, out StoredFileVisibility visibility);
    bool IsManagedKey(string? storageKey);
}
