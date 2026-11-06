using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;

namespace GrandCinema.Application.Features.Reservations.Queries;

public sealed class GetTicketHandler : IQueryHandler<GetTicketQuery, Result<TicketDto>>
{
    private readonly ITicketQueries _ticketQueries;

    public GetTicketHandler(ITicketQueries ticketQueries)
    {
        _ticketQueries = ticketQueries;
    }

    public async Task<Result<TicketDto>> Handle(GetTicketQuery query, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketQueries.GetTicketAsync(query.ReservationId, query.UserId, cancellationToken);
        if (ticket is null)
        {
            return ReservationErrors.TicketNotFound;
        }

        return ticket;
    }
}
