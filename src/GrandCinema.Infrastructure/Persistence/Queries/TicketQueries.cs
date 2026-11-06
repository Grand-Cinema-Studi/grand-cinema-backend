using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;

namespace GrandCinema.Infrastructure.Persistence.Queries;

public sealed class TicketQueries : ITicketQueries
{
    private readonly GrandCinemaDbContext _db;

    public TicketQueries(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public Task<TicketDto?> GetTicketAsync(Guid reservationId, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Step 24d");

    public Task<IReadOnlyList<TicketSummaryDto>> GetTicketsOfUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Step 24d");
}
