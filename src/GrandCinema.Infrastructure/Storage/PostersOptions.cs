using Microsoft.Extensions.Configuration;

namespace GrandCinema.Infrastructure.Storage;

public sealed class PostersOptions
{
    public const string SectionName = "Posters";
    public const string S3 = "S3";
    public const string Local = "Local";

    // "S3" by default (appsettings.json), "Local" on the developer's computer (appsettings.Development.json)
    public required string Storage { get; init; }
    // S3 only: the private bucket read by CloudFront (Parameter Store on the acceptance environment)
    public string BucketName { get; init; } = "";
    public string Region { get; init; } = "eu-west-3";
    // Local only: the folder of the posters, ignored by Git, served by the API under /posters
    public string LocalFolder { get; init; } = "";

    public static PostersOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        return section["Storage"] switch
        {
            S3 => new PostersOptions
            {
                Storage = S3,
                BucketName = section["BucketName"] ?? throw new InvalidOperationException($"Configuration value '{SectionName}:BucketName' is missing."),
                Region = section["Region"] ?? "eu-west-3"
            },
            Local => new PostersOptions { Storage = Local, LocalFolder = Path.GetFullPath(section["LocalFolder"] ?? "posters-dev") },
            _ => throw new InvalidOperationException($"Configuration value '{SectionName}:Storage' must be '{S3}' or '{Local}'.")
        };
    }
}
