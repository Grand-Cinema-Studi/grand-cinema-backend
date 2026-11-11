using GrandCinema.Application.Features.Screenings.DTOs.Mappings;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Features.Movies.DTOs.Mappings;

public static class MovieMappings
{
    public static MovieDto ToMovieDto(this Movie movie) => new()
    {
        MovieId = movie.Id,
        Title = movie.Title,
        Synopsis = movie.Synopsis,
        DurationMinutes = movie.DurationMinutes,
        Genre = movie.Genre,
        ReleaseDate = movie.ReleaseDate,
        PosterUrl = movie.PosterUrl
    };

    public static MovieDetailsDto ToMovieDetailsDto(this Movie movie, IEnumerable<Screening> screenings, IReadOnlyDictionary<Guid, int> takenSeats) => new()
    {
        MovieId = movie.Id,
        Title = movie.Title,
        Synopsis = movie.Synopsis,
        DurationMinutes = movie.DurationMinutes,
        Genre = movie.Genre,
        ReleaseDate = movie.ReleaseDate,
        PosterUrl = movie.PosterUrl,
        Days = screenings
            .GroupBy(s => DateOnly.FromDateTime(s.StartsAt))
            .OrderBy(day => day.Key)
            .Select(day => new ScreeningDayDto
            {
                Date = day.Key,
                Screenings = day
                    .OrderBy(s => s.StartsAt)
                    .Select(s => s.ToScreeningDto(takenSeats.GetValueOrDefault(s.Id)))
                    .ToList()
            })
            .ToList()
    };
}
