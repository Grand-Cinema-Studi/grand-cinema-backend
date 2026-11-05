using System;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Reservations.DTOs;

namespace GrandCinema.Application.Features.Reservations.Queries;

public class GetReservationTrackingHandler : IQueryHandler<GetReservationTrackingQuery, ReservationTrackingDto>
{
    private readonly IReservationTrackingQueries _reservationTrackingQueries;
    private readonly TimeProvider _timeProvider;

    public GetReservationTrackingHandler(IReservationTrackingQueries reservationTrackingQueries, TimeProvider timeProvider)
    {
        _reservationTrackingQueries = reservationTrackingQueries;
        _timeProvider = timeProvider;
    }

    public Task<ReservationTrackingDto> Handle(GetReservationTrackingQuery query, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
