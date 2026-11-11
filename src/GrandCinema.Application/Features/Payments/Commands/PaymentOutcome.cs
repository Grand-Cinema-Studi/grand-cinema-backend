namespace GrandCinema.Application.Features.Payments.Commands;

public enum PaymentOutcome
{
    Confirmed,
    AlreadyConfirmed,
    Refunded,
    // Another event of Stripe (payment page opened, expired…)
    Ignored
}
