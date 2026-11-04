using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Programming.DTOs;

namespace GrandCinema.Application.Features.Programming.Queries;

public sealed class GetProgrammingHandler : IQueryHandler<GetProgrammingQuery, ProgrammingDto>
{

    private readonly IProgrammingRepository _programmingRepository;
    private readonly IMovieReadRepository _movieReadRepository;
    private readonly TimeProvider _timeProvider;

    public GetProgrammingHandler(IProgrammingRepository programmingRepository, IMovieReadRepository movieReadRepository, TimeProvider timeProvider)
    {
        _programmingRepository = programmingRepository;
        _movieReadRepository = movieReadRepository;
        _timeProvider = timeProvider;
    }

    public Task<ProgrammingDto> Handle(GetProgrammingQuery query, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
