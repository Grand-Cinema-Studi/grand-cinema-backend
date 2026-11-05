using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;
using GrandCinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrandCinema.Infrastructure.Persistence.Queries;

public sealed class ReservationTrackingQueries : IReservationTrackingQueries
{
    private readonly GrandCinemaDbContext _db;

    public ReservationTrackingQueries(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ScreeningTrackingDto>> GetScreeningsOfDayAsync(DateOnly date, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);

        // One SQL query: each count is a subquery, computed by PostgreSQL; no reservation is loaded.
        // A cancelled screening is left out by the global filter (US 5.2).
        return await _db.Screenings
            .AsNoTracking()
            .Where(s => s.StartsAt >= start && s.StartsAt < end)
            .OrderBy(s => s.StartsAt)
            .ThenBy(s => s.Room.Name)
            .Select(s => new ScreeningTrackingDto
            {
                ScreeningId = s.Id,
                StartsAt = s.StartsAt,
                MovieTitle = s.Movie.Title,
                RoomName = s.Room.Name,
                Capacity = s.Room.Capacity,
                SoldOnline = _db.ReservedSeats.Count(rs => rs.ScreeningId == s.Id
                    && rs.Reservation.Status == ReservationStatus.Confirmed && rs.Reservation.PaymentReference != null),
                SoldAtCounter = _db.ReservedSeats.Count(rs => rs.ScreeningId == s.Id
                    && rs.Reservation.Status == ReservationStatus.Confirmed && rs.Reservation.PaymentReference == null),
                HeldSeats = _db.ReservedSeats.Count(rs => rs.ScreeningId == s.Id
                    && rs.Reservation.Status == ReservationStatus.PendingPayment && rs.Reservation.ExpiresAt > nowUtc)
            })
            .ToListAsync(cancellationToken);
    }
}
