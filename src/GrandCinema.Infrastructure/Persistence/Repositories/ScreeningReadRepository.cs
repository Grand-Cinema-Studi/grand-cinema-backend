using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class ScreeningReadRepository : IScreeningReadRepository
{
    private readonly GrandCinemaDbContext _db;

    public ScreeningReadRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Screening>> GetScreeningsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);

        return await _db.Screenings
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .Where(s => s.StartsAt >= start && s.StartsAt < end)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountTakenSeatsAsync(IReadOnlyCollection<Guid> screeningIds, DateTime now, CancellationToken cancellationToken = default)
    {
        // A seat is taken when it is sold, or held by a payment whose 10 minutes have not run out.
        // An expired hold or a cancelled reservation frees it, even before its lines are deleted (US 2.3).
        return await _db.ReservedSeats
            .AsNoTracking()
            .Where(rs => screeningIds.Contains(rs.ScreeningId))
            .Where(rs => rs.Reservation.Status == ReservationStatus.Confirmed
                || (rs.Reservation.Status == ReservationStatus.PendingPayment && rs.Reservation.ExpiresAt > now))
            .GroupBy(rs => rs.ScreeningId)
            .Select(group => new { ScreeningId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.ScreeningId, row => row.Count, cancellationToken);
    }

    public Task<IReadOnlyList<Screening>> GetUpcomingScreeningsOfMovieAsync(Guid movieId, DateTime from, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
