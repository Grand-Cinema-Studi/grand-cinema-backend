using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Programming.DTOs;

namespace GrandCinema.Application.Features.Programming.Queries;

public sealed class GetProgrammingHandler : IQueryHandler<GetProgrammingQuery, ProgrammingDto>
{

    private readonly IProgrammingRepository _programmingRepository;
    private readonly IScreeningReadRepository _screeningReadRepository;
    private readonly TimeProvider _timeProvider;

    public GetProgrammingHandler(IProgrammingRepository programmingRepository, IScreeningReadRepository screeningReadRepository, TimeProvider timeProvider)
    {
        _programmingRepository = programmingRepository;
        _screeningReadRepository = screeningReadRepository;
        _timeProvider = timeProvider;
    }

    public Task<ProgrammingDto> Handle(GetProgrammingQuery query, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
