using GrandCinema.Application.Abstractions.Payments;

namespace GrandCinema.Api.Tests;

public sealed class FakePaymentGateway : IPaymentGateway
{
    public const string ValidSignature = "signed-by-the-fake-stripe";

    public Task<string> CreateCheckoutAsync(PaymentRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://checkout.example/{request.ReservationId}");

    // The payload of a test notification is the identifier of the paid reservation
    public PaymentNotification ReadNotification(string payload, string signature) =>
        signature == ValidSignature
            ? PaymentNotification.Paid(Guid.Parse(payload), $"pi_{payload}")
            : PaymentNotification.NotGenuine;

    public Task RefundAsync(string paymentReference, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
