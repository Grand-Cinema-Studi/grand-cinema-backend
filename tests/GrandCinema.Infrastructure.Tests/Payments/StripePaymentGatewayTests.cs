using System.Security.Cryptography;
using System.Text;
using GrandCinema.Application.Abstractions.Payments;
using GrandCinema.Infrastructure.Payments;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Stripe;

namespace GrandCinema.Infrastructure.Tests.Payments;

// The real library of Stripe, without any network: only the check of the signature is tested here
public class StripePaymentGatewayTests
{
    private const string WebhookSecret = "whsec_secret_of_the_tests_only";

    private readonly StripePaymentGateway _gateway = new(
        new StripeOptions { SecretKey = "sk_test_not_used", WebhookSecret = WebhookSecret, FrontBaseUrl = "https://front.example" },
        new StripeClient("sk_test_not_used"),
        TimeProvider.System);

    // The notification of a paid payment page, reduced to the fields that the gateway reads
    private static string CompletedCheckout(Guid reservationId) => """
        {"id":"evt_test","object":"event","type":"checkout.session.completed","data":{"object":{"id":"cs_test","object":"checkout.session","client_reference_id":"RESERVATION_ID","payment_intent":"pi_test","payment_status":"paid"}}}
        """.Replace("RESERVATION_ID", reservationId.ToString());

    // The Stripe-Signature header: "t=<timestamp>,v1=<HMAC-SHA256 of "<timestamp>.<payload>" with the secret>"
    private static string Sign(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        return $"t={timestamp},v1={Convert.ToHexStringLower(hash)}";
    }

    // A gateway whose Stripe client goes through the fake handler: the real library, without any network
    private static StripePaymentGateway GatewayAnsweredBy(FakeStripeHandler stripe, TimeProvider time) => new(
        new StripeOptions { SecretKey = "sk_test_not_used", WebhookSecret = WebhookSecret, FrontBaseUrl = "https://front.example" },
        new StripeClient("sk_test_not_used", httpClient: new SystemNetHttpClient(new HttpClient(stripe))),
        time);

    [Fact]
    [Trait("US", "3.1")]
    public void When_stripe_signs_a_completed_payment_Then_the_paid_reservation_is_read()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var payload = CompletedCheckout(reservationId);

        // Act
        var notification = _gateway.ReadNotification(payload, Sign(payload, WebhookSecret));

        // Assert
        notification.IsGenuine.ShouldBeTrue();
        notification.PaidReservationId.ShouldBe(reservationId);
        notification.PaymentReference.ShouldBe("pi_test");
    }

    [Fact]
    [Trait("US", "3.1")]
    public void When_the_notification_is_signed_with_another_secret_Then_it_is_not_genuine()
    {
        // Arrange: the right content, signed by someone who does not know the secret
        var payload = CompletedCheckout(Guid.NewGuid());

        // Act
        var notification = _gateway.ReadNotification(payload, Sign(payload, "whsec_of_an_attacker"));

        // Assert
        notification.ShouldBe(PaymentNotification.NotGenuine);
    }

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_a_payment_page_is_created_Then_stripe_receives_the_amount_in_cents_the_reservation_and_the_return_addresses()
    {
        // Arrange: 19.00 € at 16:00 UTC
        var now = new DateTimeOffset(2026, 7, 18, 16, 0, 0, TimeSpan.Zero);
        var stripe = new FakeStripeHandler("""{"id":"cs_test","object":"checkout.session","url":"https://checkout.stripe.com/c/pay/cs_test"}""");
        var gateway = GatewayAnsweredBy(stripe, new FakeTimeProvider(now));
        var reservationId = Guid.NewGuid();

        // Act
        var url = await gateway.CreateCheckoutAsync(new PaymentRequest(reservationId, 19m, "Le Grand Cinéma – 2 place(s)"));

        // Assert: the page of Stripe, for this reservation, this amount, and 30 minutes at most
        url.ShouldBe("https://checkout.stripe.com/c/pay/cs_test");
        stripe.Request.ShouldNotBeNull().RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe("/v1/checkout/sessions");
        stripe.Body.ShouldContain($"client_reference_id={reservationId}");
        stripe.Body.ShouldContain("line_items[0][price_data][unit_amount]=1900");
        stripe.Body.ShouldContain("line_items[0][price_data][currency]=eur");
        stripe.Body.ShouldContain($"success_url=https://front.example/reservations/{reservationId}?payment=success");
        stripe.Body.ShouldContain($"cancel_url=https://front.example/reservations/{reservationId}");
        stripe.Body.ShouldContain($"expires_at={now.AddMinutes(30).ToUnixTimeSeconds()}");
    }

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_a_late_payment_is_refunded_Then_stripe_receives_its_payment_reference()
    {
        // Arrange
        var stripe = new FakeStripeHandler("""{"id":"re_test","object":"refund"}""");
        var gateway = GatewayAnsweredBy(stripe, TimeProvider.System);

        // Act
        await gateway.RefundAsync("pi_late");

        // Assert
        stripe.Request.ShouldNotBeNull().RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe("/v1/refunds");
        stripe.Body.ShouldContain("payment_intent=pi_late");
    }

    [Fact]
    [Trait("US", "3.1")]
    public void When_stripe_signs_another_event_Then_it_is_ignored()
    {
        // Arrange: genuine, but a payment page that expired without payment
        var payload = """{"id":"evt_test","object":"event","type":"checkout.session.expired","data":{"object":{"id":"cs_test","object":"checkout.session","client_reference_id":"0b6c1f8e-7d3a-4e2b-9a51-3f0c2d4e5a61","payment_status":"unpaid"}}}""";

        // Act
        var notification = _gateway.ReadNotification(payload, Sign(payload, WebhookSecret));

        // Assert
        notification.ShouldBe(PaymentNotification.Ignored);
    }
}
