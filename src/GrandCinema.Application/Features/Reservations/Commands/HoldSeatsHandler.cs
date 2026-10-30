using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;

namespace GrandCinema.Application.Features.Reservations.Commands;

public sealed class HoldSeatsHandler : ICommandHandler<HoldSeatsCommand, Result<ReservationDto>>
{
    // Decision of the Product Owner (US 2.3, criterion 7): one customer cannot block a whole room for 10 minutes.
    public const int MaximumSeatCount = 10;

    private readonly IScreeningReadRepository _screeningReadRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly TimeProvider _timeProvider;

    public HoldSeatsHandler(IScreeningReadRepository screeningReadRepository, IReservationRepository reservationRepository, TimeProvider timeProvider)
    {
        _screeningReadRepository = screeningReadRepository;
        _reservationRepository = reservationRepository;
        _timeProvider = timeProvider;
    }

    public Task<Result<ReservationDto>> Handle(HoldSeatsCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Step 17f");
}
