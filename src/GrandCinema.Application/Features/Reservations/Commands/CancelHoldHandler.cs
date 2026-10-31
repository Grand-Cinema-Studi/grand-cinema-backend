using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Reservations.Commands;

public sealed class CancelHoldHandler : ICommandHandler<CancelHoldCommand, Result<Guid>>
{
    private readonly IReservationRepository _reservationRepository;

    public CancelHoldHandler(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public Task<Result<Guid>> Handle(CancelHoldCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
