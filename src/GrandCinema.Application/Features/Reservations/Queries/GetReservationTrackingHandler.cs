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

    public async Task<ReservationTrackingDto> Handle(GetReservationTrackingQuery query, CancellationToken cancellationToken = default)
    {
        // The day of the cinema; the holds expire at an instant in UTC
        var date = query.Date ?? DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var screenings = await _reservationTrackingQueries.GetScreeningsOfDayAsync(date, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return new ReservationTrackingDto { Date = date, Screenings = screenings };
    }
}
