namespace GrandCinema.Application.Features.Programming.DTOs;

public record class ProgrammingMovieDto
{
    public required Guid MovieId { get; init; }
    public required string Title { get; init; }
    public required int DurationMinutes { get; init; }
}
