using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Abstractions.Persistence;

public interface IReservationRepository
{
    /// <summary>
    /// Holds seatCount free seats of the screening for the user, in one transaction that locks the screening:
    /// two holds on the same screening are handled one after the other. Expired holds are released first.
    /// Returns null when fewer seats are free.
    /// </summary>
    Task<Reservation?> TryHoldSeatsAsync(Guid userId, Screening screening, int seatCount, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>The reservation with its seats, or null.</summary>
    Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);
}
