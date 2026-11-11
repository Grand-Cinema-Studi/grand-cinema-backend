namespace GrandCinema.Application.Features.Reservations.DTOs;

public record class ReservationTrackingDto
{
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<ScreeningTrackingDto> Screenings { get; init; }
}
