using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Features.Programming.DTOs.Mappings;

public static class ProgrammingMappings
{
    public static ProgrammedScreeningDto ToProgrammedScreeningDto(this Screening screening, int takenSeats) => new()
    {
        ScreeningId = screening.Id,
        MovieId = screening.MovieId,
        MovieTitle = screening.Movie.Title,
        RoomId = screening.RoomId,
        RoomName = screening.Room.Name,
        StartsAt = screening.StartsAt,
        Price = screening.Price,
        TakenSeats = takenSeats,
        Capacity = screening.Room.Capacity
    };

    public static ProgrammingMovieDto ToProgrammingMovieDto(this Movie movie) => new()
    {
        MovieId = movie.Id,
        Title = movie.Title,
        DurationMinutes = movie.DurationMinutes
    };

    public static ProgrammingRoomDto ToProgrammingRoomDto(this Room room) => new()
    {
        RoomId = room.Id,
        Name = room.Name,
        Capacity = room.Capacity
    };
}
