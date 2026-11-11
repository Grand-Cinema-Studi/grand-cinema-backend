namespace GrandCinema.Application.Features.Payments.Commands;

// The body of the notification, exactly as received (the signature is computed on it), and its Stripe-Signature header
public sealed record class ConfirmPaymentCommand(string Payload, string Signature);
