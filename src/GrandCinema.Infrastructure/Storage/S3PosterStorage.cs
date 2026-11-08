using Amazon.S3;
using Amazon.S3.Model;
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

    public async Task<string> SaveAsync(byte[] content, PosterFormat format, CancellationToken cancellationToken = default)
    {
        // A random name, never the one sent by the browser: no file is overwritten, no path is made up
        var key = $"posters/{Guid.NewGuid():N}{format.Extension}";
        using var stream = new MemoryStream(content);
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = format.ContentType
        };
        request.Headers.CacheControl = CacheControl;
        await _s3.PutObjectAsync(request, cancellationToken);
        return $"/{key}";
    }
}
