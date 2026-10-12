namespace GrandCinema.Application.Features.Screenings.DTOs;

public record class ScheduleDto
{
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<MovieScheduleDto> Movies { get; init; }
}
