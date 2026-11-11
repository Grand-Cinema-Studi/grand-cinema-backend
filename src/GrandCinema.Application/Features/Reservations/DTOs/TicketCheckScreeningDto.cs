namespace GrandCinema.Application.Features.Reservations.DTOs;

public record class TicketCheckScreeningDto
{
    public required Guid ScreeningId { get; init; }
    public required DateTime StartsAt { get; init; }
    public required string MovieTitle { get; init; }
    public required string RoomName { get; init; }
}
