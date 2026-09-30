using System.Buffers.Binary;

namespace Tattoo_Project.Security;

public static class WebPFastPathRules
{
    public static bool IsSafeCanonicalWebP(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 20 ||
            !bytes[..4].SequenceEqual("RIFF"u8) ||
            !bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
            return false;

        var declaredLength = (long)BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4, 4)) + 8;
        if (declaredLength != bytes.Length) return false;

        var offset = 12;
        var hasPixelData = false;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < 8) return false;
            var chunkName = bytes.Slice(offset, 4);
            var chunkLength = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset + 4, 4));
            if (chunkName.SequenceEqual("EXIF"u8) ||
                chunkName.SequenceEqual("XMP "u8) ||
                chunkName.SequenceEqual("ICCP"u8) ||
                chunkName.SequenceEqual("ANIM"u8) ||
                chunkName.SequenceEqual("ANMF"u8))
                return false;

            var isVp8 = chunkName.SequenceEqual("VP8 "u8);
            var isVp8L = chunkName.SequenceEqual("VP8L"u8);
            var isVp8X = chunkName.SequenceEqual("VP8X"u8);
            var isAlpha = chunkName.SequenceEqual("ALPH"u8);
            if (!isVp8 && !isVp8L && !isVp8X && !isAlpha) return false;
            hasPixelData |= isVp8 || isVp8L;

            // VP8X feature flags: ICC (0x20), EXIF (0x08), XMP (0x04), animation
            // (0x02). Alpha (0x10) is image data and remains allowed.
            if (isVp8X)
            {
                if (chunkLength < 10 || bytes.Length - offset < 18) return false;
                var featureFlags = bytes[offset + 8];
                if ((featureFlags & 0x2E) != 0) return false;
            }

            var paddedLength = (long)chunkLength + (chunkLength & 1u);
            var next = (long)offset + 8 + paddedLength;
            if (next > bytes.Length) return false;
            offset = (int)next;
        }

        return offset == bytes.Length && hasPixelData;
    }
}
