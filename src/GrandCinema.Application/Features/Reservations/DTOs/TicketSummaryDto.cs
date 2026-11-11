namespace GrandCinema.Application.Features.Reservations.DTOs;

public record class TicketSummaryDto
{
    public required Guid ReservationId { get; init; }
    public required string MovieTitle { get; init; }
    public required DateTime StartsAt { get; init; }
    public required string RoomName { get; init; }
    public required int SeatCount { get; init; }
    public required bool ScreeningCancelled { get; init; }
}
