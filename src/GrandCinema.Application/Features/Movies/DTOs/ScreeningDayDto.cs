using GrandCinema.Application.Features.Screenings.DTOs;

namespace GrandCinema.Application.Features.Movies.DTOs;

public class ScreeningDayDto
{
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<ScreeningDto> Screenings { get; init; }
}
