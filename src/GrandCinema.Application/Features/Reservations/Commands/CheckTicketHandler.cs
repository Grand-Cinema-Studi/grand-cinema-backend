using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;

namespace GrandCinema.Application.Features.Reservations.Commands;

public class CheckTicketHandler : ICommandHandler<CheckTicketCommand, Result<TicketCheckDto>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ITicketQueries _ticketQueries;
    private readonly TimeProvider _timeProvider;

    public CheckTicketHandler(IReservationRepository reservationRepository, ITicketQueries ticketQueries, TimeProvider timeProvider)
    {
        _reservationRepository = reservationRepository;
        _ticketQueries = ticketQueries;
        _timeProvider = timeProvider;
    }

    public Task<Result<TicketCheckDto>> Handle(CheckTicketCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
