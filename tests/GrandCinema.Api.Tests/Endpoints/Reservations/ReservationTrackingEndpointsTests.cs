using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Reservations;

[Collection(ApiCollection.Name)]
public class ReservationTrackingEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public ReservationTrackingEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    // A screening on a day no other test uses, in its own room of 6 seats (row A):
    // A1 and A2 sold online, A3 sold at the counter, A4 held for 5 more minutes, A5 and A6 free
    private async Task<Guid> ScreeningWithReservationsAsync(DateTime startsAt)
    {
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = 6 };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Tracking contract", Synopsis = "Synopsis", DurationMinutes = 100, Genre = "Drama", ReleaseDate = new DateOnly(2030, 1, 1), PosterUrl = "https://example.com/poster.jpg" };
        var screening = new Screening { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9.5m };
        var seats = Enumerable.Range(1, 6).Select(number => new Seat { Id = Guid.NewGuid(), Room = room, Row = "A", Number = number }).ToList();
        Reservation Of(ReservationStatus status, string? paymentReference, DateTime? expiresAt, params Seat[] held)
        {
            var reservation = new Reservation { Id = Guid.NewGuid(), ScreeningId = screening.Id, Status = status, CreatedAt = DateTime.UtcNow, ExpiresAt = expiresAt, TotalAmount = 9.5m * held.Length, PaymentReference = paymentReference };
            foreach (var seat in held)
            {
                reservation.ReservedSeats.Add(new ReservedSeat { SeatId = seat.Id, ScreeningId = screening.Id });
            }
            return reservation;
        }

        await using var db = _api.CreateDbContext();
        db.Seats.AddRange(seats);
        db.Screenings.Add(screening);
        await db.SaveChangesAsync();
        db.Reservations.AddRange(
            Of(ReservationStatus.Confirmed, "pi_online", null, seats[0], seats[1]),
            Of(ReservationStatus.Confirmed, null, null, seats[2]),
            Of(ReservationStatus.PendingPayment, null, DateTime.UtcNow.AddMinutes(5), seats[3]));
        await db.SaveChangesAsync();
        return screening.Id;
    }

    [Fact]
    [Trait("US", "5.3")]
    [Trait("Category", "Integration")]
    public async Task When_the_manager_follows_a_day_Then_each_screening_shows_its_seats_sold_online_at_the_counter_held_and_remaining()
    {
        // Arrange
        var screeningId = await ScreeningWithReservationsAsync(new DateTime(2032, 4, 1, 20, 30, 0, DateTimeKind.Unspecified));
        using var manager = await _api.CreateClientWithRoleAsync(UserRole.Manager);

        // Act
        var tracking = await manager.GetFromJsonAsync<JsonElement>("/api/reservation-tracking?date=2032-04-01");

        // Assert: criterion 1, and the names the front relies on
        tracking.GetProperty("date").GetString().ShouldBe("2032-04-01");
        var screening = tracking.GetProperty("screenings").EnumerateArray().ShouldHaveSingleItem();
        screening.GetProperty("screeningId").GetString().ShouldBe(screeningId.ToString());
        screening.GetProperty("startsAt").GetString().ShouldBe("2032-04-01T20:30:00");
        screening.GetProperty("movieTitle").GetString().ShouldBe("Tracking contract");
        screening.GetProperty("roomName").GetString().ShouldStartWith("Room ");
        screening.GetProperty("capacity").GetInt32().ShouldBe(6);
        screening.GetProperty("soldOnline").GetInt32().ShouldBe(2);
        screening.GetProperty("soldAtCounter").GetInt32().ShouldBe(1);
        screening.GetProperty("heldSeats").GetInt32().ShouldBe(1);
        screening.GetProperty("remainingSeats").GetInt32().ShouldBe(2);
    }

    [Fact]
    [Trait("US", "5.3")]
    [Trait("Category", "Integration")]
    public async Task When_a_customer_follows_the_reservations_Then_403()
    {
        // Arrange
        using var customer = await _api.CreateClientWithRoleAsync(UserRole.Customer);

        // Act
        var response = await customer.GetAsync("/api/reservation-tracking?date=2032-04-02");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("US", "5.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_cashier_follows_a_day_Then_200()
    {
        // Arrange
        await ScreeningWithReservationsAsync(new DateTime(2032, 4, 3, 20, 30, 0, DateTimeKind.Unspecified));
        using var cashier = await _api.CreateClientWithRoleAsync(UserRole.Cashier);

        // Act
        var response = await cashier.GetAsync("/api/reservation-tracking?date=2032-04-03");

        // Assert: criterion 1, the seats sold online and those left
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var screening = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("screenings").EnumerateArray().ShouldHaveSingleItem();
        screening.GetProperty("soldOnline").GetInt32().ShouldBe(2);
        screening.GetProperty("remainingSeats").GetInt32().ShouldBe(2);
    }
}
