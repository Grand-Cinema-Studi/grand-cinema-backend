using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Programming.Commands;

public sealed class UpdateScreeningHandler : ICommandHandler<UpdateScreeningCommand, Result<Guid>>
{
    private readonly IProgrammingRepository _programmingRepository;
    private readonly IScreeningReadRepository _screeningReadRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateScreeningHandler(IProgrammingRepository programmingRepository, IScreeningReadRepository screeningReadRepository, TimeProvider timeProvider)
    {
        _programmingRepository = programmingRepository;
        _screeningReadRepository = screeningReadRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(UpdateScreeningCommand command, CancellationToken cancellationToken = default)
    {
        var nowLocal = _timeProvider.GetLocalNow().DateTime;
        if (ScreeningRules.Validate(command.StartsAt, command.Price, nowLocal) is { } invalid)
        {
            return invalid;
        }

        var screening = await _programmingRepository.GetScreeningAsync(command.ScreeningId, cancellationToken);
        if (screening is null)
        {
            return ProgrammingErrors.ScreeningNotFound;
        }
        if (screening.StartsAt <= nowLocal)
        {
            return ProgrammingErrors.ScreeningStarted;
        }
        if (!command.Confirmed && await HasTakenSeatsAsync(screening.Id, cancellationToken))
        {
            return ProgrammingErrors.ScreeningHasReservations;
        }

        // Its current slot does not count: a screening can be moved by a few minutes
        var (from, to) = ScreeningRules.NeighbourhoodOf(command.StartsAt);
        var neighbours = await _programmingRepository.GetScreeningsOfRoomAsync(screening.RoomId, from, to, cancellationToken);
        if (neighbours.Any(other => other.Id != screening.Id && other.OverlapsWith(command.StartsAt, screening.Movie.DurationMinutes)))
        {
            return ProgrammingErrors.RoomNotAvailable;
        }

        await _programmingRepository.UpdateScreeningAsync(screening.Id, command.StartsAt, command.Price, cancellationToken);
        return screening.Id;
    }

    // Seats held (not expired) or sold, counted as the remaining seats of the schedule
    private async Task<bool> HasTakenSeatsAsync(Guid screeningId, CancellationToken cancellationToken)
    {
        var takenSeats = await _screeningReadRepository.CountTakenSeatsAsync([screeningId], _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return takenSeats.GetValueOrDefault(screeningId) > 0;
    }
}