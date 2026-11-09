using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Abstractions.Storage;

namespace GrandCinema.Application.Features.Programming.Commands;

public sealed class UpdateMovieHandler : ICommandHandler<UpdateMovieCommand, Result<Guid>>
{
    private readonly IProgrammingRepository _programmingRepository;
    private readonly IMovieReadRepository _movieReadRepository;
    private readonly IPosterStorage _posterStorage;
    private readonly TimeProvider _timeProvider;

    public UpdateMovieHandler(IProgrammingRepository programmingRepository, IMovieReadRepository movieReadRepository, IPosterStorage posterStorage, TimeProvider timeProvider)
    {
        _programmingRepository = programmingRepository;
        _movieReadRepository = movieReadRepository;
        _posterStorage = posterStorage;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(UpdateMovieCommand command, CancellationToken cancellationToken = default)
    {
        if (MovieRules.Validate(command.Title, command.Synopsis, command.DurationMinutes, command.Genre, command.ReleaseDate, command.Poster, posterRequired: false) is { } invalid)
        {
            return invalid;
        }

        var movie = await _movieReadRepository.GetByIdAsync(command.MovieId, cancellationToken);
        if (movie is null)
        {
            return MovieErrors.MovieNotFound;
        }
        if (movie.ArchivedAt is not null)
        {
            return ProgrammingErrors.MovieArchived;
        }

        // Checked above: every field is there
        var title = command.Title!.Trim();
        var releaseDate = command.ReleaseDate!.Value;
        if (await _programmingRepository.OtherMovieExistsAsync(movie.Id, title, releaseDate, cancellationToken))
        {
            return ProgrammingErrors.MovieAlreadyExists;
        }

        // The screenings not ended were placed with the current duration: a longer movie could overlap the next screening of its room
        var durationMinutes = command.DurationMinutes!.Value;
        if (durationMinutes != movie.DurationMinutes
            && await _programmingRepository.HasScreeningsNotEndedAsync(movie.Id, _timeProvider.GetLocalNow().DateTime.AddMinutes(-movie.DurationMinutes), cancellationToken))
        {
            return ProgrammingErrors.MovieHasScreenings;
        }

        // A new poster, a new name: the old file stays, no cache to invalidate
        if (command.Poster is { } poster)
        {
            movie.PosterUrl = await _posterStorage.SaveAsync(poster, PosterRules.Detect(poster)!, cancellationToken);
        }
        movie.Title = title;
        movie.Synopsis = command.Synopsis!.Trim();
        movie.DurationMinutes = durationMinutes;
        movie.Genre = command.Genre!.Trim();
        movie.ReleaseDate = releaseDate;
        await _programmingRepository.UpdateMovieAsync(movie, cancellationToken);
        return movie.Id;
    }
}
