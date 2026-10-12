using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Features.Screenings.DTOs.Mappings;

internal static class ScreenMappings
{
    public static MovieScheduleDto ToMovieScheduleDto(this Movie movie, IEnumerable<Screening> screenings) => new()
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
            .Select(s => s.ToScreeningDto())
            .ToList()
    };

    public static ScreeningDto ToScreeningDto(this Screening screening) => new()
    {
        ScreeningId = screening.Id,
        RoomName = screening.Room.Name,
        StartsAt = screening.StartsAt,
        Price = screening.Price
    };
}
