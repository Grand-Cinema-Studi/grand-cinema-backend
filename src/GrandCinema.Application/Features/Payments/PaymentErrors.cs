using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Payments;

public static class PaymentErrors
{
    // Not signed by Stripe: anyone could otherwise declare a reservation paid. 400, as Stripe recommends.
    public static readonly Error NotificationNotGenuine = Error.Validation(new Dictionary<string, string[]>
    {
        ["Stripe-Signature"] = ["The notification is not signed by Stripe."]
    });
}
