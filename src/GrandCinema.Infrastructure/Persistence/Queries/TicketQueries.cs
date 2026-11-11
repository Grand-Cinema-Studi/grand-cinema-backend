using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;
using GrandCinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrandCinema.Infrastructure.Persistence.Queries;

public sealed class TicketQueries : ITicketQueries
{
    private readonly GrandCinemaDbContext _db;

    public TicketQueries(GrandCinemaDbContext db)
    {
        _db = db;
    }

    // Paid reservations of the account, with their ticket. The screenings cancelled after the payment stay (US 5.2)
    private IQueryable<Domain.Entities.Reservation> PaidOf(Guid userId) =>
        _db.Reservations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId && r.Status == ReservationStatus.Confirmed && r.TicketCode != null);

    public async Task<TicketDto?> GetTicketAsync(Guid reservationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await PaidOf(userId)
            .Where(r => r.Id == reservationId)
            .Select(r => new TicketDto
            {
                ReservationId = r.Id,
                TicketCode = r.TicketCode!,
                MovieTitle = r.Screening.Movie.Title,
                StartsAt = r.Screening.StartsAt,
                RoomName = r.Screening.Room.Name,
                Seats = r.ReservedSeats
                    .OrderBy(reserved => reserved.Seat.Row.Length)
                    .ThenBy(reserved => reserved.Seat.Row)
                    .ThenBy(reserved => reserved.Seat.Number)
                    .Select(reserved => reserved.Seat.Row + reserved.Seat.Number.ToString())
                    .ToList(),
                ScreeningCancelled = r.Screening.CancelledAt != null
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketSummaryDto>> GetTicketsOfUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await PaidOf(userId)
            .OrderByDescending(r => r.Screening.StartsAt)
            .Select(r => new TicketSummaryDto
            {
                ReservationId = r.Id,
                MovieTitle = r.Screening.Movie.Title,
                StartsAt = r.Screening.StartsAt,
                RoomName = r.Screening.Room.Name,
                SeatCount = r.ReservedSeats.Count,
                ScreeningCancelled = r.Screening.CancelledAt != null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TicketCheckDto?> GetCheckedTicketAsync(string ticketCode, CancellationToken cancellationToken = default)
    {
        return await _db.Reservations
            .AsNoTracking()
            .Where(r => r.TicketCode == ticketCode && r.Status == ReservationStatus.Confirmed)
            .Select(r => new TicketCheckDto
            {
                MovieTitle = r.Screening.Movie.Title,
                StartsAt = r.Screening.StartsAt,
                RoomName = r.Screening.Room.Name,
                Seats = r.ReservedSeats
                    .OrderBy(reserved => reserved.Seat.Row.Length)
                    .ThenBy(reserved => reserved.Seat.Row)
                    .ThenBy(reserved => reserved.Seat.Number)
                    .Select(reserved => reserved.Seat.Row + reserved.Seat.Number.ToString())
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketCheckScreeningDto>> GetScreeningsOfDayAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);

        // Started ones included (late spectators); a cancelled one is left out by the global filter (US 5.2)
        return await _db.Screenings
            .AsNoTracking()
            .Where(s => s.StartsAt >= start && s.StartsAt < end)
            .OrderBy(s => s.StartsAt)
            .ThenBy(s => s.Room.Name)
            .Select(s => new TicketCheckScreeningDto
            {
                ScreeningId = s.Id,
                StartsAt = s.StartsAt,
                MovieTitle = s.Movie.Title,
                RoomName = s.Room.Name
            })
            .ToListAsync(cancellationToken);
    }
}
