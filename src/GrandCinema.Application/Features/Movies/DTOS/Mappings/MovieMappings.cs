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
}
