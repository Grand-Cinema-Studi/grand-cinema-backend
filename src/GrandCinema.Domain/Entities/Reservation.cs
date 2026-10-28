using GrandCinema.Domain.Enums;

namespace GrandCinema.Domain.Entities;

public class Reservation
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; } = null!;
    public Guid ScreeningId { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal TotalAmount { get; set; }
    public ICollection<ReservedSeat> ReservedSeats { get; set; } = new List<ReservedSeat>();
    public Screening Screening { get; set; } = null!;
}
