using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Movies.DTOs;

namespace GrandCinema.Application.Features.Movies.Queries;

public sealed class GetMoviesShowingHandler : IQueryHandler<GetMoviesShowingQuery, IReadOnlyList<MovieDto>>
{
    private readonly IMovieReadRepository _movieReadRepository;
    private readonly TimeProvider _timeProvider;

    public GetMoviesShowingHandler(IMovieReadRepository movieReadRepository, TimeProvider timeProvider)
    {
        this._movieReadRepository = movieReadRepository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<MovieDto>> Handle(GetMoviesShowingQuery query, CancellationToken cancellationToken = default) 
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        await _movieReadRepository.GetMoviesScheduledFromAsync(now, cancellationToken);

        return [];
    }
}
