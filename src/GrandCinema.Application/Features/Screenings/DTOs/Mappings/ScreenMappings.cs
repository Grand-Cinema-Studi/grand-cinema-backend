using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Features.Screenings.DTOs.Mappings;

internal static class ScreenMappings
{
    public static MovieScheduleDto ToMovieScheduleDto(this Movie movie, IEnumerable<Screening> screenings, IReadOnlyDictionary<Guid, int> takenSeats) => new()
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
            // A screening without any seat taken is absent from the counts: all its seats remain
            .Select(s => s.ToScreeningDto(takenSeats.GetValueOrDefault(s.Id)))
            .ToList()
    };

    public static ScreeningDto ToScreeningDto(this Screening screening, int takenSeats) => new()
    {
        ScreeningId = screening.Id,
        RoomName = screening.Room.Name,
        StartsAt = screening.StartsAt,
        Price = screening.Price,
        RemainingSeats = screening.Room.Capacity - takenSeats
    };
}
