namespace GrandCinema.Application.Abstractions.Persistence;

public enum HoldRefusal
{
    // Fewer free seats than asked, also for the second of two customers who want the last seat
    NotEnoughSeats,
    // The customer would have more than 10 seats, held or paid, for the screening
    SeatLimitReached
}
