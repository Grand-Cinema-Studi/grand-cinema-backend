using GrandCinema.Application.Abstractions.Security;
using GrandCinema.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GrandCinema.Infrastructure.Security;

public sealed class JwtAccessTokenGenerator : IAccessTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public JwtAccessTokenGenerator(JwtOptions options, TimeProvider timeProvider)
    {
        _options = options;
        _timeProvider = timeProvider;
    }

    public AccessToken Generate(User user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.ExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [AppClaimTypes.UserId] = user.Id.ToString(),
                [AppClaimTypes.Email] = user.Email,
                [AppClaimTypes.Role] = user.Role.ToString(),
                [AppClaimTypes.TokenId] = Guid.NewGuid().ToString()
            },
            SigningCredentials = new SigningCredentials(_options.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expiresAt);
    }
}
