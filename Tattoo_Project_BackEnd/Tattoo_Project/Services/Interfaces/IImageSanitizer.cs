using Microsoft.AspNetCore.Http;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces;

public sealed record SanitizedImage(byte[] Bytes, string Extension, string ContentType, int Width, int Height);

public interface IImageSanitizer
{
    Task<ResultService<SanitizedImage>> SanitizeAsync(IFormFile file, long maxBytes, long maxPixels, CancellationToken cancellationToken = default);
}
