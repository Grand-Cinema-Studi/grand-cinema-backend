using Amazon.S3;
using Amazon.S3.Model;
using GrandCinema.Application.Abstractions.Storage;
using GrandCinema.Infrastructure.Storage;
using Moq;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Storage;

public class S3PosterStorageTests
{
    [Fact]
    [Trait("US", "5.6")]
    public async Task When_a_poster_is_saved_Then_it_is_put_under_posters_with_its_type_and_a_cache_of_a_year()
    {
        // Arrange: S3 itself is replaced, the request sent to it is kept
        PutObjectRequest? sent = null;
        var s3 = new Mock<IAmazonS3>();
        s3.Setup(client => client.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new PutObjectResponse());
        var options = new PostersOptions { Storage = PostersOptions.S3, BucketName = "posters-bucket" };

        // Act
        var url = await new S3PosterStorage(s3.Object, options).SaveAsync([0x89, 0x50, 0x4E, 0x47], PosterFormat.Png);

        // Assert: the address the front displays is the key of the file, which CloudFront serves under /posters
        sent.ShouldNotBeNull();
        sent.BucketName.ShouldBe("posters-bucket");
        sent.Key.ShouldStartWith("posters/");
        sent.Key.ShouldEndWith(".png");
        url.ShouldBe($"/{sent.Key}");
        sent.ContentType.ShouldBe("image/png");
        sent.Headers.CacheControl.ShouldBe("public, max-age=31536000, immutable");
    }
}
