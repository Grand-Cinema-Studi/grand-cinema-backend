using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Screenings.DTOs;

namespace GrandCinema.Application.Features.Screenings.Queries;

public sealed class GetScheduleHandler : IQueryHandler<GetScheduleQuery, ScheduleDto>
{
    private readonly IScreeningReadRepository _screeningReadRepository;
    private readonly TimeProvider _timeProvider;

    public GetScheduleHandler(IScreeningReadRepository screeningReadRepository, TimeProvider timeProvider)
    {
        _screeningReadRepository = screeningReadRepository;
        _timeProvider = timeProvider;
    }

    public Task<ScheduleDto> Handle(GetScheduleQuery query, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
