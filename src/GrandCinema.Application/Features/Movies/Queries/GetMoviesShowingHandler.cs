using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Movies.DTOS;

namespace GrandCinema.Application.Features.Movies.Queries;

public sealed class GetMoviesShowingHandler : IQueryHandler<GetMoviesShowingQuery, IReadOnlyList<MovieDto>>
{
    private readonly IMovieReadRepository movieReadRepository;
    private readonly TimeProvider _timeProvider;

    public GetMoviesShowingHandler(IMovieReadRepository movieReadRepository, TimeProvider timeProvider)
    {
        this.movieReadRepository = movieReadRepository;
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<MovieDto>> Handle(GetMoviesShowingQuery query, CancellationToken cancellationToken = default) 
        => throw new NotImplementedException();
}
