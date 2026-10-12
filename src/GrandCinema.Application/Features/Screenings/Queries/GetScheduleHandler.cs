using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Screenings.DTOs;
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
        var screenings = await _screeningReadRepository.GetScreeningsAsync(query.Date, cancellationToken);
        var movies = screenings
            .GroupBy(s => s.Movie.Id)
            .Select(group => ToMovieScheduleDto(group.First().Movie, group))
            .OrderBy(m => m.Title)
            .ToList();

        return new ScheduleDto
        {
            Date = query.Date,
            Movies = movies
        };
    }

    private static MovieScheduleDto ToMovieScheduleDto(Movie movie, IEnumerable<Screening> screenings) => new ()
    {
        MovieId = movie.Id,
        Title = movie.Title,
        Synopsis = movie.Synopsis,
        DurationMinutes = movie.DurationMinutes,
        Genre = movie.Genre,
        ReleaseDate = movie.ReleaseDate,
        PosterUrl = movie.PosterUrl,
        Screenings = screenings
            .OrderBy(s => s.StartsAt)
            .Select(ToScreeningDto)
            .ToList()
    };

    private static ScreeningDto ToScreeningDto(Screening screening) => new ()
    {
        ScreeningId = screening.Id,
        RoomName = screening.Room.Name,
        StartsAt = screening.StartsAt,
        Price = screening.Price
    };
};
