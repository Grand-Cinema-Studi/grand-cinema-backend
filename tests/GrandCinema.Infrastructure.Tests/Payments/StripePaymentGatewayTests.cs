using System.Security.Cryptography;
using System.Text;
using GrandCinema.Application.Abstractions.Payments;
using GrandCinema.Infrastructure.Payments;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Payments;

// The real library of Stripe, without any network: only the check of the signature is tested here
public class StripePaymentGatewayTests
{
    private const string WebhookSecret = "whsec_secret_of_the_tests_only";

    private readonly StripePaymentGateway _gateway = new(
        new StripeOptions { SecretKey = "sk_test_not_used", WebhookSecret = WebhookSecret, FrontBaseUrl = "https://front.example" },
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
}
