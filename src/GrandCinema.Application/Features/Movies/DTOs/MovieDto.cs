namespace GrandCinema.Application.Features.Movies.DTOs;

public class MovieDto
{
    public required Guid MovieId { get; init; }
    public required string Title { get; init; }
    public required string Synopsis { get; init; }
    public required int DurationMinutes { get; init; }
    public required string Genre { get; init; }
    public required DateOnly ReleaseDate { get; init; }
    public required string PosterUrl { get; init; }
}
