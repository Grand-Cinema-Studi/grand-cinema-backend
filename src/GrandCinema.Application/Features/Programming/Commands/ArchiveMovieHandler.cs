using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Programming.Commands;

public sealed class ArchiveMovieHandler : ICommandHandler<ArchiveMovieCommand, Result<Guid>>
{
    private readonly IProgrammingRepository _programmingRepository;
    private readonly IMovieReadRepository _movieReadRepository;
    private readonly TimeProvider _timeProvider;

    public ArchiveMovieHandler(IProgrammingRepository programmingRepository, IMovieReadRepository movieReadRepository, TimeProvider timeProvider)
    {
        _programmingRepository = programmingRepository;
        _movieReadRepository = movieReadRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(ArchiveMovieCommand command, CancellationToken cancellationToken = default)
    {
        var movie = await _movieReadRepository.GetByIdAsync(command.MovieId, cancellationToken);
        if (movie is null)
        {
            return MovieErrors.MovieNotFound;
        }
        // Archiving twice changes nothing: the first date stays
        if (movie.ArchivedAt is not null)
        {
            return movie.Id;
        }

        // A movie still shown stays: once archived, it has no screening to come, and the public pages hide it by themselves
        if (await _programmingRepository.HasScreeningsNotEndedAsync(movie.Id, _timeProvider.GetLocalNow().DateTime.AddMinutes(-movie.DurationMinutes), cancellationToken))
        {
            return ProgrammingErrors.MovieHasScreenings;
        }

        await _programmingRepository.ArchiveMovieAsync(movie.Id, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return movie.Id;
    }
}
