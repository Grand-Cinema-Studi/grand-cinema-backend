using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;
using GrandCinema.Domain.Entities;

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

    public async Task<Result<TicketCheckDto>> Handle(CheckTicketCommand command, CancellationToken cancellationToken = default)
    {
        // A QR code of something else, or badly read: refused without querying the database (criterion 3)
        if (!Reservation.IsTicketCode(command.TicketCode))
        {
            return ReservationErrors.TicketNotFound;
        }

        var use = await _reservationRepository.UseTicketAsync(
            command.TicketCode!, command.ScreeningId, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        switch (use)
        {
            case TicketUse.AlreadyUsed:
                return ReservationErrors.TicketAlreadyUsed;
            case TicketUse.OtherScreening:
                return ReservationErrors.TicketForAnotherScreening;
            case TicketUse.NotFound:
                return ReservationErrors.TicketNotFound;
        }

        // Let in: what the inspector checks at a glance (movie, seats)
        var ticket = await _ticketQueries.GetCheckedTicketAsync(command.TicketCode!, cancellationToken);
        return ticket is null ? ReservationErrors.TicketNotFound : ticket;
    }
}
