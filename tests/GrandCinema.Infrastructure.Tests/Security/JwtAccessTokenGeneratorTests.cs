using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using GrandCinema.Infrastructure.Security;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Security;

public class JwtAccessTokenGeneratorTests
{
    private readonly JwtOptions _options = new()
    {
        Issuer = "grand-cinema-api",
        Audience = "grand-cinema-front",
        SigningKey = "test-signing-key-at-least-32-bytes-long",
        ExpirationMinutes = 60
    };

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));

    private readonly User _manager = new()
    {
        Id = Guid.NewGuid(),
        Email = "manager@example.com",
        PasswordHash = "hashed-password",
        FirstName = "Paul",
        LastName = "Durand",
        Role = UserRole.Manager
    };

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_a_token_is_generated_Then_it_is_signed_and_identifies_the_user_and_their_role()
    {
        // Arrange
        var generator = new JwtAccessTokenGenerator(_options, _time);

        // Act
        var token = generator.Generate(_manager);

        // Assert
        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = _options.CreateSigningKey(),
            // The fake clock is in the past: lifetime is checked by the next test.
            ValidateLifetime = false
        });
        validation.IsValid.ShouldBeTrue();
        var jwt = validation.SecurityToken.ShouldBeOfType<JsonWebToken>();
        jwt.Subject.ShouldBe(_manager.Id.ToString());
        jwt.GetClaim(AppClaimTypes.Role).Value.ShouldBe("Manager");
    }

    [Fact]
    [Trait("US", "2.1")]
    public void When_a_token_is_generated_Then_it_expires_after_the_configured_delay()
    {
        // Arrange
        var generator = new JwtAccessTokenGenerator(_options, _time);

        // Act
        var token = generator.Generate(_manager);

        // Assert
        var expected = new DateTime(2026, 7, 18, 11, 0, 0, DateTimeKind.Utc);
        token.ExpiresAt.ShouldBe(expected);
        new JsonWebTokenHandler().ReadJsonWebToken(token.Value).ValidTo.ShouldBe(expected);
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_a_token_is_checked_with_another_key_Then_it_is_rejected()
    {
        // Arrange
        var generator = new JwtAccessTokenGenerator(_options, _time);
        var token = generator.Generate(_manager);
        var otherKey = new SymmetricSecurityKey("another-signing-key-at-least-32-bytes"u8.ToArray());

        // Act
        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = otherKey,
            ValidateLifetime = false
        });

        // Assert
        validation.IsValid.ShouldBeFalse();
    }
}
