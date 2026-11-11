namespace GrandCinema.Application.Features.Movies.DTOs;

public class MovieDetailsDto
{
    public required Guid MovieId { get; init; }
    public required string Title { get; init; }
    public required string Synopsis { get; init; }
    public required int DurationMinutes { get; init; }
    public required string Genre { get; init; }
    public required DateOnly ReleaseDate { get; init; }
    public required string PosterUrl { get; init; }
    // Upcoming screenings: one entry per day in date order, each day sorted by time
    public required IReadOnlyList<ScreeningDayDto> Days { get; init; }
}
