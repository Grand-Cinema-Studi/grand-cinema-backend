using Amazon.S3;
using GrandCinema.Application.Abstractions.Storage;

namespace GrandCinema.Infrastructure.Storage;

// Acceptance environment: a private bucket read by CloudFront only (/posters/*); the server may only add files under posters/
public sealed class S3PosterStorage : IPosterStorage
{
    // A new name for every poster: a file never changes, so CloudFront and the browsers keep it for a year
    public const string CacheControl = "public, max-age=31536000, immutable";

    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;

    public S3PosterStorage(IAmazonS3 s3, PostersOptions options)
    {
        _s3 = s3;
        _bucketName = options.BucketName;
    }

    public Task<string> SaveAsync(byte[] content, PosterFormat format, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Step 27d");
}
