namespace GrandCinema.Application.Features.Reservations.DTOs;

public record class TicketDto
{
    public required Guid ReservationId { get; init; }
    public required string TicketCode { get; init; }
    public required string MovieTitle { get; init; }
    public required DateTime StartsAt { get; init; }
    public required string RoomName { get; init; }
    public required IReadOnlyList<string> Seats { get; init; }
    // The screening cancelled after the payment
    public required bool ScreeningCancelled { get; init; }
}
