using GrandCinema.Domain.Enums;

namespace GrandCinema.Domain.Entities;

public class Reservation
{
    // EPIC 2 rule: seats are held while the customer pays, then freed after 10 minutes (US 2.3).
    public static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(10);

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

    /// <summary>Holds seats for a signed-in customer while they pay: not a sale yet (US 2.3, criterion 3).</summary>
    public static Reservation Hold(Guid userId, Screening screening, IReadOnlyCollection<Seat> seats, DateTime nowUtc) =>
        throw new NotImplementedException("Step 17e");

    /// <summary>A hold whose 10 minutes have run out frees its seats, even before it is marked "Expired".</summary>
    public bool HasExpired(DateTime nowUtc) => throw new NotImplementedException("Step 17e");
}
