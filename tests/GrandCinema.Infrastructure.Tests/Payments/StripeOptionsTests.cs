using GrandCinema.Infrastructure.Payments;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Payments;

public class StripeOptionsTests
{
    private readonly Dictionary<string, string?> _settings = new()
    {
        ["Stripe:SecretKey"] = "sk_test_key",
        ["Stripe:WebhookSecret"] = "whsec_secret",
        ["Stripe:FrontBaseUrl"] = "https://front.example/"
    };

    private StripeOptions Read() =>
        StripeOptions.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection(_settings).Build());

    [Fact]
    [Trait("US", "3.1")]
    public void When_the_configuration_is_complete_Then_every_value_is_read_and_the_front_address_has_no_final_slash()
    {
        // Act
        var options = Read();

        // Assert: "https://front.example/" would give "//reservations/…" in the return addresses
        options.SecretKey.ShouldBe("sk_test_key");
        options.WebhookSecret.ShouldBe("whsec_secret");
        options.FrontBaseUrl.ShouldBe("https://front.example");
    }

    [Fact]
    [Trait("US", "3.1")]
    public void When_the_webhook_secret_is_missing_Then_the_error_names_it()
    {
        // Arrange
        _settings.Remove("Stripe:WebhookSecret");

        // Act and assert: a deployment without this parameter fails at once, and says why
        Should.Throw<InvalidOperationException>(Read).Message.ShouldContain("Stripe:WebhookSecret");
    }
}
