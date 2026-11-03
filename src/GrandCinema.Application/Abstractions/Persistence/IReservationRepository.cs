using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Abstractions.Persistence;

public interface IReservationRepository
{
    /// <summary>
    /// Holds seatCount free seats of the screening for the user, in one transaction that locks the screening:
    /// two holds on the same screening are handled one after the other. Expired holds are released first.
    /// Returns the reason when the hold is refused.
    /// </summary>
    Task<HoldResult> TryHoldSeatsAsync(Guid userId, Screening screening, int seatCount, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>The reservation with its seats, or null.</summary>
    Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a hold waiting for payment and frees its seats at once.
    /// Returns false when the reservation is not waiting for payment (paid, expired and cleaned, already cancelled).
    /// </summary>
    Task<bool> CancelHoldAsync(Guid reservationId, CancellationToken cancellationToken = default);
    /// <summary>Records that the customer accepted the terms of sale (version and instant) before paying.</summary>
    Task RecordTermsOfSaleAsync(Guid reservationId, string version, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a paid reservation under the lock of its screening, the same as the holds, which release the expired seats:
    /// Confirmed while its seats are still held, AlreadyConfirmed for the same payment notified twice, Rejected otherwise.
    /// </summary>
    Task<PaymentConfirmation> ConfirmPaymentAsync(Guid reservationId, string paymentReference, DateTime nowUtc, CancellationToken cancellationToken = default);
}
