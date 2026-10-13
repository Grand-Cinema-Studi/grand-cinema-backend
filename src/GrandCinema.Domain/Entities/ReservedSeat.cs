namespace GrandCinema.Domain.Entities;

public class ReservedSeat
{
    public Guid ReservationId { get; set; }
    public Guid ScreeningId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public Guid SeatId { get; set; }
    public Seat Seat { get; set; } = null!;
}
