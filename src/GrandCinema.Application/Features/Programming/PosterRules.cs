using GrandCinema.Application.Abstractions.Storage;

namespace GrandCinema.Application.Features.Programming;

public static class PosterRules
{
    public const int MaxBytes = 2 * 1024 * 1024;

    // The format is read from the first bytes of the file (its signature), never from its name nor from what the browser declares
    public static PosterFormat? Detect(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(JpegSignature))
        {
            return PosterFormat.Jpeg;
        }
        if (content.StartsWith(PngSignature))
        {
            return PosterFormat.Png;
        }
        if (content.Length >= 12 && content.StartsWith("RIFF"u8) && content.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return PosterFormat.Webp;
        }
        return null;
    }

    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
}
