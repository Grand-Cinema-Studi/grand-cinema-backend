using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly GrandCinemaDbContext _db;

    public ReservationRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public Task<Reservation?> TryHoldSeatsAsync(Guid userId, Screening screening, int seatCount, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Step 17h");

    public Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Step 17h");
}
