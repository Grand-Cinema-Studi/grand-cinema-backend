namespace GrandCinema.Application.Features.Reservations.DTOs;

public record class ReservationDto
{
    public required Guid ReservationId { get; init; }
    public required Guid ScreeningId { get; init; }
    // The movie of the screening: the front goes back to its page (cancel, or seats freed)
    public required Guid MovieId { get; init; }
    // "PendingPayment" while the customer pays, "Expired" once the 10 minutes have run out, "Confirmed" once paid (US 3.1)
    public required string Status { get; init; }
    // Seats given by the cinema ("C4", "C5"): the choice on a seat map is US 2.4 (V2)
    public required IReadOnlyList<string> Seats { get; init; }
    public required decimal TotalAmount { get; init; }
    // Instant in UTC ("Z"): the front shows the time left before the seats are freed
    public required DateTime? ExpiresAt { get; init; }
}
