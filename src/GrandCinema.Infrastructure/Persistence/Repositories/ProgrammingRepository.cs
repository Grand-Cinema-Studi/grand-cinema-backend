using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Programming.DTOs;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class ProgrammingRepository : IProgrammingRepository
{
    private readonly GrandCinemaDbContext _db;

    public ProgrammingRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Movie>> GetMoviesAsync(CancellationToken cancellationToken = default) =>
        await _db.Movies.AsNoTracking().OrderBy(m => m.Title).ToListAsync(cancellationToken);

    public async Task<bool> MovieExistsAsync(string title, DateOnly releaseDate, CancellationToken cancellationToken = default)
    {
        // upper() on both sides, run by PostgreSQL: the case of the title is ignored
        var normalized = title.ToUpperInvariant();
        return await _db.Movies.AnyAsync(m => m.ReleaseDate == releaseDate && m.Title.ToUpper() == normalized, cancellationToken);
    }

    public async Task AddMovieAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetRoomsAsync(CancellationToken cancellationToken = default) =>
        await _db.Rooms.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);
    public async Task<Room?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
    public async Task<Screening?> GetScreeningAsync(Guid screeningId, CancellationToken cancellationToken = default) =>
        await _db.Screenings.AsNoTracking().Include(s => s.Movie).FirstOrDefaultAsync(s => s.Id == screeningId, cancellationToken);
    public async Task<IReadOnlyList<Screening>> GetScreeningsOfRoomAsync(Guid roomId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        await _db.Screenings
            .AsNoTracking()
            .Include(s => s.Movie)
            .Where(s => s.RoomId == roomId && s.StartsAt >= from && s.StartsAt < to)
            .ToListAsync(cancellationToken);
    public async Task AddScreeningAsync(Screening screening, CancellationToken cancellationToken = default)
    {
        _db.Screenings.Add(screening);
        await _db.SaveChangesAsync(cancellationToken);
    }
    public async Task UpdateScreeningAsync(Guid screeningId, DateTime startsAt, decimal price, CancellationToken cancellationToken = default)
    {
        await _db.Screenings
            .Where(s => s.Id == screeningId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.StartsAt, startsAt)
                .SetProperty(s => s.Price, price), cancellationToken);
    }
    public async Task CancelScreeningAsync(Guid screeningId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // The lock of the holds and of the payments (bug 0001): none of them runs on this screening at the same time
        await _db.Database.ExecuteSqlAsync(
            $"""SELECT 1 FROM "Screenings" WHERE "Id" = {screeningId} FOR UPDATE""", cancellationToken);

        await _db.Screenings
            .Where(s => s.Id == screeningId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.CancelledAt, nowUtc), cancellationToken);

        // The holds waiting for payment are cancelled and their seats freed: a payment arriving later is refunded (US 3.1).
        // The sales keep their seats: history (US 5.3, 5.4) and refunds.
        var holds = await _db.Reservations
            .Where(r => r.ScreeningId == screeningId && r.Status == ReservationStatus.PendingPayment)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
        if (holds.Count > 0)
        {
            await _db.ReservedSeats
                .Where(reserved => holds.Contains(reserved.ReservationId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.Reservations
                .Where(r => holds.Contains(r.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, ReservationStatus.Cancelled), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
