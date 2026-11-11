using GrandCinema.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Security;

public class JwtOptionsTests
{
    private readonly Dictionary<string, string?> _settings = new()
    {
        ["Jwt:Issuer"] = "grand-cinema-api",
        ["Jwt:Audience"] = "grand-cinema-front",
        ["Jwt:SigningKey"] = new string('k', JwtOptions.MinimumSigningKeyBytes),
        ["Jwt:ExpirationMinutes"] = "30"
    };

    private JwtOptions Read() =>
        JwtOptions.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection(_settings).Build());

    [Fact]
    [Trait("US", "2.1")]
    public void When_the_configuration_is_complete_Then_every_value_is_read()
    {
        // Act
        var options = Read();

        // Assert
        options.Issuer.ShouldBe("grand-cinema-api");
        options.Audience.ShouldBe("grand-cinema-front");
        options.SigningKey.ShouldBe(new string('k', JwtOptions.MinimumSigningKeyBytes));
        options.ExpirationMinutes.ShouldBe(30);
    }

    [Theory]
    [Trait("US", "2.1")]
    [InlineData("Issuer")]
    [InlineData("Audience")]
    [InlineData("SigningKey")]
    public void When_a_required_value_is_missing_Then_the_application_refuses_to_start_and_names_it(string key)
    {
        // Arrange
        _settings.Remove($"Jwt:{key}");

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => Read());

        // Assert
        exception.Message.ShouldContain($"Jwt:{key}");
    }

    [Fact]
    [Trait("US", "2.1")]
    public void When_the_signing_key_is_shorter_than_32_bytes_Then_the_application_refuses_to_start()
    {
        // Arrange
        _settings["Jwt:SigningKey"] = new string('k', JwtOptions.MinimumSigningKeyBytes - 1);

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => Read());

        // Assert
        exception.Message.ShouldContain("Jwt:SigningKey");
    }

    [Fact]
    [Trait("US", "2.1")]
    public void When_the_expiration_is_not_configured_Then_a_token_lasts_60_minutes()
    {
        // Arrange
        _settings.Remove("Jwt:ExpirationMinutes");

        // Act
        var options = Read();

        // Assert
        options.ExpirationMinutes.ShouldBe(60);
    }
}
