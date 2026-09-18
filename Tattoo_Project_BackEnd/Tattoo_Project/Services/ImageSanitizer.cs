using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public sealed class ImageSanitizer : IImageSanitizer
{
    public async Task<ResultService<SanitizedImage>> SanitizeAsync(IFormFile file, long maxBytes, long maxPixels, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length <= 0) return ResultService<SanitizedImage>.Fail("Image is required.");
        if (file.Length > maxBytes) return ResultService<SanitizedImage>.Fail("Image file is too large.");
        try
        {
            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input, cancellationToken);
            var pixels = checked((long)image.Width * image.Height);
            if (image.Width <= 0 || image.Height <= 0 || pixels > maxPixels)
                return ResultService<SanitizedImage>.Fail("Image dimensions are too large.");

            // Strip metadata by clearing profiles and re-encoding. The original bytes are never served.
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;
            await using var output = new MemoryStream();
            await image.SaveAsync(output, new WebpEncoder { Quality = 90 }, cancellationToken);
            return ResultService<SanitizedImage>.Ok(new SanitizedImage(output.ToArray(), ".webp", "image/webp", image.Width, image.Height));
        }
        catch (UnknownImageFormatException)
        {
            return ResultService<SanitizedImage>.Fail("Uploaded file is not a supported image.");
        }
        catch (InvalidImageContentException)
        {
            return ResultService<SanitizedImage>.Fail("Uploaded image is invalid or corrupted.");
        }
        catch (OverflowException)
        {
            return ResultService<SanitizedImage>.Fail("Image dimensions are invalid.");
        }
    }
}
