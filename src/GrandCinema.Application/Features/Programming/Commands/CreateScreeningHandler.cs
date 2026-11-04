using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Features.Programming.Commands;

public class CreateScreeningHandler : ICommandHandler<CreateScreeningCommand, Result<Guid>>
{
    private readonly IProgrammingRepository _programmingRepository;
    private readonly IMovieReadRepository _movieReadRepository;
    private readonly TimeProvider _timeProvider;

    public CreateScreeningHandler(IProgrammingRepository programmingRepository, IMovieReadRepository movieReadRepository, TimeProvider timeProvider)
    {
        _programmingRepository = programmingRepository;
        _movieReadRepository = movieReadRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateScreeningCommand command, CancellationToken cancellationToken = default)
    {
        if (ScreeningRules.Validate(command.StartsAt, command.Price, _timeProvider.GetLocalNow().DateTime) is { } invalid)
        {
            return invalid;
        }

        var movie = await _movieReadRepository.GetByIdAsync(command.MovieId, cancellationToken);
        if (movie is null)
        {
            return MovieErrors.MovieNotFound;
        }

        var room = await _programmingRepository.GetRoomAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return ProgrammingErrors.RoomNotFound;
        }

        var (from, to) = ScreeningRules.NeighbourhoodOf(command.StartsAt);
        var neighbours = await _programmingRepository.GetScreeningsOfRoomAsync(room.Id, from, to, cancellationToken);
        if (neighbours.Any(other => other.OverlapsWith(command.StartsAt, movie.DurationMinutes)))
        {
            return ProgrammingErrors.RoomNotAvailable;
        }

        var screening = new Screening { Id = Guid.NewGuid(), MovieId = movie.Id, RoomId = room.Id, StartsAt = command.StartsAt, Price = command.Price };
        await _programmingRepository.AddScreeningAsync(screening, cancellationToken);
        return screening.Id;
    }
}
