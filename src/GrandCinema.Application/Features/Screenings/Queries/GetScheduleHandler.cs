using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Screenings.DTOs;
using GrandCinema.Application.Features.Screenings.DTOs.Mappings;

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

    public async Task<ScheduleDto> Handle(GetScheduleQuery query, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        var date = query.Date ?? DateOnly.FromDateTime(now);
        // A screening that has started is not held (HoldSeatsHandler). It is no longer offered either
        var screenings = (await _screeningReadRepository.GetScreeningsAsync(date, cancellationToken))
            .Where(s => s.StartsAt > now)
            .ToList();
        var takenSeats = await _screeningReadRepository.CountTakenSeatsAsync(screenings.Select(s => s.Id).ToList(), _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        var movies = screenings
            .GroupBy(s => s.Movie.Id)
            .Select(group => group.First().Movie.ToMovieScheduleDto(group, takenSeats))
            .OrderBy(m => m.Title)
            .ToList();

        return new ScheduleDto
        {
            Date = date,
            Movies = movies
        };
    }
};
