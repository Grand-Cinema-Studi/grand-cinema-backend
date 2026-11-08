using GrandCinema.Application.Abstractions.Storage;
using GrandCinema.Application.Features.Programming;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Programming;

public class PosterRulesTests
{
    [Theory]
    [Trait("US", "5.6")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }, "image/jpeg", ".jpg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 }, "image/png", ".png")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x56 }, "image/webp", ".webp")]
    public void When_a_file_starts_like_a_jpeg_png_or_webp_image_Then_its_format_is_detected(byte[] content, string contentType, string extension)
    {
        // Act
        var format = PosterRules.Detect(content);

        // Assert
        format.ShouldBe(new PosterFormat(contentType, extension));
    }

    [Theory]
    [Trait("US", "5.6")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })] // "GIF89a"
    [InlineData(new byte[] { 0x3C, 0x73, 0x76, 0x67, 0x20 })] // "<svg ": text, which may hold a script
    public void When_a_file_is_not_a_jpeg_png_or_webp_image_Then_no_format_is_detected(byte[] content)
    {
        // Act
        var format = PosterRules.Detect(content);

        // Assert
        format.ShouldBeNull();
    }
}
