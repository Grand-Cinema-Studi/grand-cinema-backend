namespace GrandCinema.Application.Features.Programming.DTOs;

public record class ProgrammingMovieDto
{
    public required Guid MovieId { get; init; }
    public required string Title { get; init; }
    public required int DurationMinutes { get; init; }
    // US 5.6: the whole card, for the form that edits it
    public required string Synopsis { get; init; }
    public required string Genre { get; init; }
    public required DateOnly ReleaseDate { get; init; }
    public required string PosterUrl { get; init; }
}
