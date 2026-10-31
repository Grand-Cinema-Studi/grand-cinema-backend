using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.DTOs.Mappings;

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

    public async Task<Result<ReservationDto>> Handle(HoldSeatsCommand command, CancellationToken cancellationToken = default)
    {
        if (command.SeatCount < 1 || command.SeatCount > MaximumSeatCount)
        {
            return Error.Validation(new Dictionary<string, string[]>
            {
                ["seatCount"] = [$"Between 1 and {MaximumSeatCount} seats can be held at once."]
            });
        }

        var screening = await _screeningReadRepository.GetByIdAsync(command.ScreeningId, cancellationToken);
        if (screening is null)
        {
            return ReservationErrors.ScreeningNotFound;
        }

        // The start time is in the local time of the cinema: a screening that starts now can no longer be booked
        if (screening.StartsAt <= _timeProvider.GetLocalNow().DateTime)
        {
            return ReservationErrors.ScreeningStarted;
        }

        // A hold expires at an instant in UTC (step 15: two clocks)
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var hold = await _reservationRepository.TryHoldSeatsAsync(command.UserId, screening, command.SeatCount, nowUtc, cancellationToken);
        if (hold is null)
        {
            return ReservationErrors.NotEnoughSeats;
        }

        return hold.ToReservationDto(screening.MovieId, nowUtc);
    }
}
