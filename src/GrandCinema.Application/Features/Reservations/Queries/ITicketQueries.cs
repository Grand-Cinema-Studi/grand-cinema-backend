using GrandCinema.Application.Features.Reservations.DTOs;

namespace GrandCinema.Application.Features.Reservations.Queries;

// ADR 0004: tickets are read straight into DTOs by PostgreSQL. Only paid reservations have one.
public interface ITicketQueries
{
    // The ticket of a paid reservation of this account, or null (not paid, unknown, or of another account)
    Task<TicketDto?> GetTicketAsync(Guid reservationId, Guid userId, CancellationToken cancellationToken = default);

    // The paid reservations of this account, the latest screening first, cancelled screenings included
    Task<IReadOnlyList<TicketSummaryDto>> GetTicketsOfUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // What the inspector sees once the ticket of this code has let its holders in
    Task<TicketCheckDto?> GetCheckedTicketAsync(string ticketCode, CancellationToken cancellationToken = default);

    // The screenings of a day, started ones included, cancelled ones excluded, by time then room
    Task<IReadOnlyList<TicketCheckScreeningDto>> GetScreeningsOfDayAsync(DateOnly date, CancellationToken cancellationToken = default);
}
