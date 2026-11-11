namespace GrandCinema.Application.Abstractions.Payments;

/// <summary>A notification of the provider: genuine or not, and the reservation it says is paid, if any.</summary>
public record class PaymentNotification(bool IsGenuine, Guid? PaidReservationId, string? PaymentReference)
{
    // Not signed by the provider: nothing it says can be trusted
    public static readonly PaymentNotification NotGenuine = new(false, null, null);

    // Genuine, but not a completed payment (page opened, page expired…)
    public static readonly PaymentNotification Ignored = new(true, null, null);

    public static PaymentNotification Paid(Guid reservationId, string paymentReference) => new(true, reservationId, paymentReference);
}
