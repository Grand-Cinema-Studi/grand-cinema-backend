using GrandCinema.Application.Abstractions.Storage;

namespace GrandCinema.Application.Features.Programming;

public static class PosterRules
{
    // 2 MB: a poster of 1000 x 1500 pixels, compressed, weighs a few hundred kilobytes
    public const int MaxBytes = 2 * 1024 * 1024;

    // The format is read from the first bytes of the file (its signature), never from its name nor from what the browser declares
    public static PosterFormat? Detect(ReadOnlySpan<byte> content) => throw new NotImplementedException("Step 27c");
}
