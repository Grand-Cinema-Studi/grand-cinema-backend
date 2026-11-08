using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Abstractions.Storage;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Features.Programming.Commands;

public class CreateMovieHandler : ICommandHandler<CreateMovieCommand, Result<Guid>>
{
    private readonly IProgrammingRepository _programmingRepository;
    private readonly IPosterStorage _posterStorage;

    public CreateMovieHandler(IProgrammingRepository programmingRepository, IPosterStorage posterStorage)
    {
        _programmingRepository = programmingRepository;
        _posterStorage = posterStorage;
    }

    public async Task<Result<Guid>> Handle(CreateMovieCommand command, CancellationToken cancellationToken = default)
    {
        if (MovieRules.Validate(command.Title, command.Synopsis, command.DurationMinutes, command.Genre, command.ReleaseDate, command.Poster, posterRequired: true) is { } invalid)
        {
            return invalid;
        }

        var title = command.Title!.Trim();
        var releaseDate = command.ReleaseDate!.Value;
        if (await _programmingRepository.MovieExistsAsync(title, releaseDate, cancellationToken))
        {
            return ProgrammingErrors.MovieAlreadyExists;
        }

        // The poster first: a movie never points to a poster that does not exist
        var poster = command.Poster!;
        var posterUrl = await _posterStorage.SaveAsync(poster, PosterRules.Detect(poster)!, cancellationToken);
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Title = title,
            Synopsis = command.Synopsis!.Trim(),
            DurationMinutes = command.DurationMinutes!.Value,
            Genre = command.Genre!.Trim(),
            ReleaseDate = releaseDate,
            PosterUrl = posterUrl
        };
        await _programmingRepository.AddMovieAsync(movie, cancellationToken);
        return movie.Id;
    }
}
