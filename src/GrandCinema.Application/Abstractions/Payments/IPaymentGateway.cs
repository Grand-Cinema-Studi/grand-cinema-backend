namespace GrandCinema.Application.Abstractions.Payments;

/// <summary>The payment provider (Stripe): the card is typed on its page, never on ours.</summary>
public interface IPaymentGateway
{
    /// <summary>Creates the payment page of a hold and returns its address.</summary>
    Task<string> CreateCheckoutAsync(PaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads a notification of the provider, after checking its signature.</summary>
    PaymentNotification ReadNotification(string payload, string signature);

    /// <summary>Gives the money back, for a payment whose seats were freed, or a second payment of the same reservation.</summary>
    Task RefundAsync(string paymentReference, CancellationToken cancellationToken = default);
}
