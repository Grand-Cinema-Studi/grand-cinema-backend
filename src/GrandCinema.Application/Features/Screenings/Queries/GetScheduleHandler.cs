using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Screenings.DTOs;
using GrandCinema.Application.Features.Screenings.DTOs.Mappings;
using GrandCinema.Domain.Entities;

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
        var date = query.Date ?? default;
        var screenings = await _screeningReadRepository.GetScreeningsAsync(date, cancellationToken);
        var movies = screenings
            .GroupBy(s => s.Movie.Id)
            .Select(group => group.First().Movie.ToMovieScheduleDto(group))
            .OrderBy(m => m.Title)
            .ToList();

        return new ScheduleDto
        {
            Date = date,
            Movies = movies
        };
    }
};
