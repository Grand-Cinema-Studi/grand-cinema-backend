using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Shouldly;

namespace GrandCinema.Domain.Tests.Entities;

public class ReservationTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 18, 0, 0, DateTimeKind.Utc);
    private readonly Screening _screening = new() { Id = Guid.NewGuid(), StartsAt = new DateTime(2026, 7, 18, 20, 30, 0, DateTimeKind.Unspecified), Price = 9.5m };

    private static Seat SeatAt(string row, int number) => new() { Id = Guid.NewGuid(), Row = row, Number = number };

    [Fact]
    [Trait("US", "2.3")]
    public void When_seats_are_held_Then_the_reservation_waits_for_payment_for_10_minutes_at_the_price_of_the_screening()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var seats = new[] { SeatAt("C", 4), SeatAt("C", 5) };

        // Act
        var hold = Reservation.Hold(userId, _screening, seats, Now);

        // Assert: a hold, not a sale (criterion 3)
        hold.UserId.ShouldBe(userId);
        hold.ScreeningId.ShouldBe(_screening.Id);
        hold.Status.ShouldBe(ReservationStatus.PendingPayment);
        hold.CreatedAt.ShouldBe(Now);
        hold.ExpiresAt.ShouldBe(Now.AddMinutes(10));
        hold.TotalAmount.ShouldBe(19m);
        hold.ReservedSeats.Select(reserved => reserved.SeatId).ShouldBe(seats.Select(seat => seat.Id));
        hold.ReservedSeats.ShouldAllBe(reserved => reserved.ScreeningId == _screening.Id && reserved.ReservationId == hold.Id);
    }

    [Fact]
    [Trait("US", "2.3")]
    public void When_the_10_minutes_have_run_out_Then_the_hold_has_expired_but_a_sale_never_expires()
    {
        // Arrange
        var hold = Reservation.Hold(Guid.NewGuid(), _screening, new[] { SeatAt("A", 1) }, Now);

        // Act, then Assert: still held one second before the end, freed at the tenth minute
        hold.HasExpired(Now.AddMinutes(10).AddSeconds(-1)).ShouldBeFalse();
        hold.HasExpired(Now.AddMinutes(10)).ShouldBeTrue();
        hold.Status = ReservationStatus.Confirmed;
        hold.HasExpired(Now.AddHours(1)).ShouldBeFalse();
    }
}
