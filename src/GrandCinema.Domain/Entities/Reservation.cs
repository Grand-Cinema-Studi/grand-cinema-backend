using GrandCinema.Domain.Enums;

namespace GrandCinema.Domain.Entities;

public class Reservation
{
    // Seats are held while the customer pays, then freed after 10 minutes.
    public static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(10);
    // At most 10 seats per customer and per screening, held or paid.
    public const int MaximumSeatsPerCustomer = 10;

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
    public static Reservation Hold(Guid userId, Screening screening, IReadOnlyCollection<Seat> seats, DateTime nowUtc)
    {
        var hold = new Reservation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            // The identifier only: the screening may come from another DbContext, it must not be inserted again
            ScreeningId = screening.Id,
            Status = ReservationStatus.PendingPayment,
            CreatedAt = nowUtc,
            ExpiresAt = nowUtc + HoldDuration,
            // The price is fixed when the seats are held: the payment (US 3.1) takes this amount
            TotalAmount = screening.Price * seats.Count
        };

        foreach (var seat in seats)
        {
            hold.ReservedSeats.Add(new ReservedSeat { ReservationId = hold.Id, ScreeningId = screening.Id, SeatId = seat.Id, Seat = seat });
        }

        return hold;
    }

    /// <summary>A hold whose 10 minutes have run out frees its seats, even before it is marked "Expired".</summary>
    public bool HasExpired(DateTime nowUtc) => Status == ReservationStatus.PendingPayment && ExpiresAt <= nowUtc;
}
