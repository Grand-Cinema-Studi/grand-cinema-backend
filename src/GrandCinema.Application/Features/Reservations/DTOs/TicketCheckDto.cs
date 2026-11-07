namespace GrandCinema.Application.Features.Reservations.DTOs;

public record class TicketCheckDto
{
    public required string MovieTitle { get; init; }
    public required DateTime StartsAt { get; init; }
    public required string RoomName { get; init; }
    // How many people the ticket lets in, and where they sit
    public required IReadOnlyList<string> Seats { get; init; }
}
