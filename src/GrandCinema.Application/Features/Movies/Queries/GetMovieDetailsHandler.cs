using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Movies.DTOs;

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

    public Task<Result<MovieDetailsDto>> Handle(GetMovieDetailsQuery query, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
