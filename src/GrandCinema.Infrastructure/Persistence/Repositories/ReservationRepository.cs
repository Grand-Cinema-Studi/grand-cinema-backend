using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly GrandCinemaDbContext _db;

    public ReservationRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public async Task<Reservation?> TryHoldSeatsAsync(Guid userId, Screening screening, int seatCount, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // 1. Lock the screening until the end of the transaction (bug 0001): a second hold on the same screening
        //    waits here, then sees the seats taken by the first one. The other screenings are not slowed down.
        await _db.Database.ExecuteSqlAsync(
            $"""SELECT 1 FROM "Screenings" WHERE "Id" = {screening.Id} FOR UPDATE""", cancellationToken);

        // 2. Release the holds of this screening whose 10 minutes have run out: their rows would otherwise
        //    keep the unique index (ScreeningId, SeatId) busy, and their seats could never be sold again.
        var expiredHolds = await _db.Reservations
            .Where(r => r.ScreeningId == screening.Id && r.Status == ReservationStatus.PendingPayment && r.ExpiresAt <= nowUtc)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
        if (expiredHolds.Count > 0)
        {
            await _db.ReservedSeats
                .Where(reserved => expiredHolds.Contains(reserved.ReservationId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.Reservations
                .Where(r => expiredHolds.Contains(r.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, ReservationStatus.Expired), cancellationToken);
        }

        // 3. The first free seats of the room: rows A to Z, then AA…, by number
        var freeSeats = await _db.Seats
            .Where(seat => seat.RoomId == screening.RoomId
                && !_db.ReservedSeats.Any(reserved => reserved.ScreeningId == screening.Id && reserved.SeatId == seat.Id))
            .OrderBy(seat => seat.Row.Length)
            .ThenBy(seat => seat.Row)
            .ThenBy(seat => seat.Number)
            .Take(seatCount)
            .ToListAsync(cancellationToken);
        if (freeSeats.Count < seatCount)
        {
            // The release of the expired holds is kept, even when this hold is refused
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        // 4. Save the hold; the commit releases the lock
        var hold = Reservation.Hold(userId, screening, freeSeats, nowUtc);
        _db.Reservations.Add(hold);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        // Last line of defence: the unique index refuses a seat sold twice, whatever the code above does (bug 0001)
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return null;
        }

        await transaction.CommitAsync(cancellationToken);
        return hold;
    }

    public async Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        return await _db.Reservations
            .AsNoTracking()
            .Include(r => r.Screening)
            .Include(r => r.ReservedSeats)
            .ThenInclude(reserved => reserved.Seat)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);
    }

    public async Task<bool> CancelHoldAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // Only a hold waiting for payment: PostgreSQL checks the status when it writes,
        // so a reservation paid in the meantime (US 3.1) is never cancelled
        var cancelled = await _db.Reservations
            .Where(r => r.Id == reservationId && r.Status == ReservationStatus.PendingPayment)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, ReservationStatus.Cancelled), cancellationToken);
        if (cancelled == 0)
        {
            return false;
        }

        // Its seats go back on sale at once: the unique index (ScreeningId, SeatId) lets another customer hold them
        await _db.ReservedSeats
            .Where(reserved => reserved.ReservationId == reservationId)
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
