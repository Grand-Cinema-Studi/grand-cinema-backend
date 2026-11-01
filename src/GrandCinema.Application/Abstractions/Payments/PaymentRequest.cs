namespace GrandCinema.Application.Abstractions.Payments;

/// <summary>What the customer pays: the amount fixed when the seats were held.</summary>
public sealed record class PaymentRequest(Guid ReservationId, decimal Amount, string Description);
