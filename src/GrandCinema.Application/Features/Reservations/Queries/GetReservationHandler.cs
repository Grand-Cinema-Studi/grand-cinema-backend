using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.DTOs.Mappings;

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

    public async Task<Result<ReservationDto>> Handle(GetReservationQuery query, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(query.ReservationId, cancellationToken);
        // The reservation of another customer answers like an unknown one: its existence is never revealed
        if (reservation is null || reservation.UserId != query.UserId)
        {
            return ReservationErrors.ReservationNotFound;
        }

        return reservation.ToReservationDto(reservation.Screening.MovieId, _timeProvider.GetUtcNow().UtcDateTime);
    }
}
