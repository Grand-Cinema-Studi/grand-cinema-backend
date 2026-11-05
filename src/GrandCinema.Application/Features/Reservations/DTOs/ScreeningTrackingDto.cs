namespace GrandCinema.Application.Features.Reservations.DTOs;

public record class ScreeningTrackingDto
{
    public required Guid ScreeningId { get; init; }
    public required DateTime StartsAt { get; init; }
    public required string MovieTitle { get; init; }
    public required string RoomName { get; init; }
    public required int Capacity { get; init; }
    // Paid on Stripe: the reservation has a payment reference
    public required int SoldOnline { get; init; }
    // Paid without any payment reference: sold at the counter
    public required int SoldAtCounter { get; init; }
    // Held by a payment in progress whose 10 minutes have not run out
    public required int HeldSeats { get; init; }

    // What is still on sale: the capacity minus the seats sold and those held
    public int RemainingSeats => Capacity - SoldOnline - SoldAtCounter - HeldSeats;
}
