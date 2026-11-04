namespace GrandCinema.Application.Features.Programming.DTOs;

public record class ProgrammingDto
{
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<ProgrammedScreeningDto> Screenings { get; init; }
    public required IReadOnlyList<ProgrammingMovieDto> Movies { get; init; }
    public required IReadOnlyList<ProgrammingRoomDto> Rooms { get; init; }
}
