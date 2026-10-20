using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GrandCinema.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 requires a key of at least 256 bits.
    public const int MinimumSigningKeyBytes = 32;

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string SigningKey { get; init; }
    public int ExpirationMinutes { get; init; } = 60;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        string Required(string key) =>
            configuration[$"{SectionName}:{key}"]
            ?? throw new InvalidOperationException($"Configuration value '{SectionName}:{key}' is missing.");

        var signingKey = Required(nameof(SigningKey));
        if (Encoding.UTF8.GetByteCount(signingKey) < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException($"Configuration value '{SectionName}:{nameof(SigningKey)}' must be at least {MinimumSigningKeyBytes} bytes long.");
        }

        return new JwtOptions
        {
            Issuer = Required(nameof(Issuer)),
            Audience = Required(nameof(Audience)),
            SigningKey = signingKey,
            ExpirationMinutes = int.TryParse(configuration[$"{SectionName}:{nameof(ExpirationMinutes)}"], out var minutes) ? minutes : 60
        };
    }
}
