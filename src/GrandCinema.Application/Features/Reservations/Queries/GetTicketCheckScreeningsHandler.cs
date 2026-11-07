using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Reservations.DTOs;

namespace GrandCinema.Application.Features.Reservations.Queries;

public sealed class GetTicketCheckScreeningsHandler : IQueryHandler<GetTicketCheckScreeningsQuery, IReadOnlyList<TicketCheckScreeningDto>>
{
    private readonly ITicketQueries _ticketQueries;
    private readonly TimeProvider _timeProvider;

    public GetTicketCheckScreeningsHandler(ITicketQueries ticketQueries, TimeProvider timeProvider)
    {
        _ticketQueries = ticketQueries;
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<TicketCheckScreeningDto>> Handle(GetTicketCheckScreeningsQuery query, CancellationToken cancellationToken = default)
    {
        // The day of the cinema, as for the reservation tracking (US 5.3)
        var date = query.Date ?? DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        return _ticketQueries.GetScreeningsOfDayAsync(date, cancellationToken);
    }
}
