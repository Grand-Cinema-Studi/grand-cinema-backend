using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Movies.DTOs;
using GrandCinema.Application.Features.Movies.DTOs.Mappings;

namespace GrandCinema.Application.Features.Movies.Queries;

public class GetFeaturedMoviesHandler : IQueryHandler<GetFeaturedMoviesQuery, IReadOnlyList<MovieDto>>
{
    public const int FeaturedCount = 4;

    private readonly IMovieReadRepository _movieReadRepository;
    private readonly TimeProvider _timeProvider;

    public GetFeaturedMoviesHandler(IMovieReadRepository movieReadRepository, TimeProvider timeProvider)
    {
        _movieReadRepository = movieReadRepository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<MovieDto>> Handle(GetFeaturedMoviesQuery query, CancellationToken cancellationToken = default)
    {
        // Today's movies first, then those of the following days and months: the carousel is full at any time of day
        var movies = await _movieReadRepository.GetMoviesByNextScreeningAsync(_timeProvider.GetLocalNow().DateTime, FeaturedCount, cancellationToken);
        return movies.Select(m => m.ToMovieDto()).ToList();
    }
}
