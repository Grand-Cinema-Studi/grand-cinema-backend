using GrandCinema.Application.Abstractions.Storage;
using GrandCinema.Infrastructure.Storage;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Storage;

public class LocalPosterStorageTests
{
    [Fact]
    [Trait("US", "5.6")]
    public async Task When_a_poster_is_saved_Then_it_is_a_new_file_of_the_folder_served_under_posters()
    {
        // Arrange: a folder of its own, deleted at the end
        var folder = Path.Combine(Path.GetTempPath(), $"posters-{Guid.NewGuid():N}");
        var options = new PostersOptions { Storage = PostersOptions.Local, LocalFolder = folder };
        try
        {
            // Act
            var url = await new LocalPosterStorage(options).SaveAsync([0xFF, 0xD8, 0xFF], PosterFormat.Jpeg);

            // Assert
            url.ShouldStartWith("/posters/");
            url.ShouldEndWith(".jpg");
            (await File.ReadAllBytesAsync(Path.Combine(folder, Path.GetFileName(url)))).ShouldBe(new byte[] { 0xFF, 0xD8, 0xFF });
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
