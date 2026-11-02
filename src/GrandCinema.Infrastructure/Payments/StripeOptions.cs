using Microsoft.Extensions.Configuration;

namespace GrandCinema.Infrastructure.Payments;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    // Test key ("sk_test_…") in every environment of the project: user secrets locally, Parameter Store on the acceptance environment
    public required string SecretKey { get; init; }
    // Shared with Stripe to sign the notifications ("whsec_…"): given by "stripe listen" locally, by the dashboard for the acceptance environment
    public required string WebhookSecret { get; init; }
    // Address of the front: Stripe sends the customer back to the page of the hold, after paying or when giving up
    public required string FrontBaseUrl { get; init; }

    public static StripeOptions FromConfiguration(IConfiguration configuration)
    {
        string Required(string key) =>
            configuration[$"{SectionName}:{key}"]
            ?? throw new InvalidOperationException($"Configuration value '{SectionName}:{key}' is missing.");

        return new StripeOptions
        {
            SecretKey = Required(nameof(SecretKey)),
            WebhookSecret = Required(nameof(WebhookSecret)),
            FrontBaseUrl = Required(nameof(FrontBaseUrl)).TrimEnd('/')
        };
    }
}
