using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Reservations.DTOs;

namespace GrandCinema.Application.Features.Reservations.Queries;

public sealed class GetMyTicketsHandler : IQueryHandler<GetMyTicketsQuery, IReadOnlyList<TicketSummaryDto>>
{
    private readonly ITicketQueries _ticketQueries;

    public GetMyTicketsHandler(ITicketQueries ticketQueries)
    {
        _ticketQueries = ticketQueries;
    }

    public Task<IReadOnlyList<TicketSummaryDto>> Handle(GetMyTicketsQuery query, CancellationToken cancellationToken = default) =>
        _ticketQueries.GetTicketsOfUserAsync(query.UserId, cancellationToken);
}
