using GrandCinema.Application.Abstractions.Payments;
using Stripe;
using Stripe.Checkout;

namespace GrandCinema.Infrastructure.Payments;

public class StripePaymentGateway : IPaymentGateway
{
    // Stripe only accepts a payment page that lives between 30 minutes and 24 hours
    private static readonly TimeSpan CheckoutLifetime = TimeSpan.FromMinutes(30);

    private readonly StripeOptions _options;
    private readonly IStripeClient _client;
    private readonly TimeProvider _timeProvider;

    public StripePaymentGateway(StripeOptions options, TimeProvider timeProvider)
    {
        _options = options;
        _client = new StripeClient(options.SecretKey);
        _timeProvider = timeProvider;
    }

    public async Task<string> CreateCheckoutAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var reservationId = request.ReservationId.ToString();
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            Locale = "fr",
            // Read back in the notification: the reservation that has been paid
            ClientReferenceId = reservationId,
            ExpiresAt = _timeProvider.GetUtcNow().UtcDateTime + CheckoutLifetime,
            SuccessUrl = $"{_options.FrontBaseUrl}/reservations/{reservationId}?payment=success",
            CancelUrl = $"{_options.FrontBaseUrl}/reservations/{reservationId}",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        // Stripe counts in cents: 19.00 € is 1900
                        UnitAmount = (long)Math.Round(request.Amount * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = request.Description }
                    }
                }
            ]
        };

        var session = await new SessionService(_client).CreateAsync(options, cancellationToken: cancellationToken);
        return session.Url;
    }

    public PaymentNotification ReadNotification(string payload, string signature) =>
        throw new NotImplementedException("Step 20h");

    public async Task RefundAsync(string paymentReference, CancellationToken cancellationToken = default)
    {
        await new RefundService(_client).CreateAsync(new RefundCreateOptions { PaymentIntent = paymentReference }, cancellationToken: cancellationToken);
    }
}
