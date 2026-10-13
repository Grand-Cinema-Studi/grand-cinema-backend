namespace GrandCinema.Application.Features.Screenings.DTOs;

public record class MovieScheduleDto
{
    public required Guid MovieId { get; init; }
    public required string Title { get; init; }
    public required string Synopsis { get; init; }
    public required int DurationMinutes { get; init; }
    public required string Genre { get; init; }
    public required DateOnly ReleaseDate { get; init; }
    public required string PosterUrl { get; init; }
    public required IReadOnlyList<ScreeningDto> Screenings { get; init; }
}
