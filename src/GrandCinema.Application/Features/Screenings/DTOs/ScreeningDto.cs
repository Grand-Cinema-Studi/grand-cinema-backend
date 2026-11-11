namespace GrandCinema.Application.Features.Screenings.DTOs;

public record class ScreeningDto
{
    public required Guid ScreeningId { get; init; }
    public required string RoomName { get; init; }
    public required DateTime StartsAt { get; init; }
    public required decimal Price { get; init; }
    public required int RemainingSeats { get; init; }
}
