using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Movies.DTOs;
using GrandCinema.Application.Features.Movies.DTOs.Mappings;

namespace GrandCinema.Application.Features.Movies.Queries;

public sealed class GetMovieDetailsHandler : IQueryHandler<GetMovieDetailsQuery, Result<MovieDetailsDto>>
{
    private readonly IMovieReadRepository _movieReadRepository;
    private readonly IScreeningReadRepository _screeningReadRepository;
    private readonly TimeProvider _timeProvider;

    public GetMovieDetailsHandler(IMovieReadRepository movieReadRepository, IScreeningReadRepository screeningReadRepository, TimeProvider timeProvider)
    {
        _movieReadRepository = movieReadRepository;
        _screeningReadRepository = screeningReadRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MovieDetailsDto>> Handle(GetMovieDetailsQuery query, CancellationToken cancellationToken = default)
    {
        var movie = await _movieReadRepository.GetByIdAsync(query.MovieId, cancellationToken);
        if (movie is null)
        {
            return MovieErrors.MovieNotFound;
        }

        // "Upcoming": the screening starts now or later, in the local time of the cinema, without any date limit (up to a year ahead and more)
        var screenings = await _screeningReadRepository.GetUpcomingScreeningsOfMovieAsync(movie.Id, _timeProvider.GetLocalNow().DateTime, cancellationToken);
        // A hold expires at an instant in UTC: compared with the current instant, in UTC too
        var takenSeats = await _screeningReadRepository.CountTakenSeatsAsync(
            screenings.Select(s => s.Id).ToList(), _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return movie.ToMovieDetailsDto(screenings, takenSeats);
    }
}
