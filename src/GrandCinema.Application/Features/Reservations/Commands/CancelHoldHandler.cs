using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Domain.Enums;

namespace GrandCinema.Application.Features.Reservations.Commands;

public sealed class CancelHoldHandler : ICommandHandler<CancelHoldCommand, Result<Guid>>
{
    private readonly IReservationRepository _reservationRepository;

    public CancelHoldHandler(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<Guid>> Handle(CancelHoldCommand command, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, cancellationToken);
        // The reservation of another customer answers like an unknown one (OWASP A01)
        if (reservation is null || reservation.UserId != command.UserId)
        {
            return ReservationErrors.ReservationNotFound;
        }

        if (reservation.Status == ReservationStatus.Confirmed)
        {
            return ReservationErrors.ReservationConfirmed;
        }

        // A hold waiting for payment is cancelled and its seats freed at once; an expired or cancelled one has nothing left to free
        await _reservationRepository.CancelHoldAsync(reservation.Id, cancellationToken);
        return reservation.Id;
    }
}
