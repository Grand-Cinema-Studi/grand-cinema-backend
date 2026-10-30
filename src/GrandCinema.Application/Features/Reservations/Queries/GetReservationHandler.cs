using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;

namespace GrandCinema.Application.Features.Reservations.Queries;

public sealed class GetReservationHandler : IQueryHandler<GetReservationQuery, Result<ReservationDto>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly TimeProvider _timeProvider;

    public GetReservationHandler(IReservationRepository reservationRepository, TimeProvider timeProvider)
    {
        _reservationRepository = reservationRepository;
        _timeProvider = timeProvider;
    }

    public Task<Result<ReservationDto>> Handle(GetReservationQuery query, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Step 17g");
}
