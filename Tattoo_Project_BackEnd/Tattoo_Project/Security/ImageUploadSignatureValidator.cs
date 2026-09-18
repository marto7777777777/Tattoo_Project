namespace Tattoo_Project.Security;

public static class ImageUploadSignatureValidator
{
    public static bool MatchesExtension(IFormFile file, string extension)
    {
        Span<byte> header = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        if (read < 3) return false;

        extension = extension.ToLowerInvariant();
        if (extension is ".jpg" or ".jpeg")
            return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        if (extension == ".png")
            return read >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        if (extension == ".webp")
            return read >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8);
        return false;
    }
}
