using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Screenings;

[Collection(ApiCollection.Name)]
public class ScheduleEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public ScheduleEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_visitor_asks_for_a_day_Then_the_movies_of_that_day_are_returned_with_their_screenings()
    {
        // Arrange: a day far in the future, on which no other test creates a screening
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = 120 };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Schedule contract", Synopsis = "Synopsis", DurationMinutes = 110, Genre = "Drama", ReleaseDate = new DateOnly(2030, 1, 1), PosterUrl = "https://example.com/poster.jpg" };
        await using (var db = _api.CreateDbContext())
        {
            db.Screenings.AddRange(
                new Screening { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = new DateTime(2031, 3, 14, 20, 30, 0, DateTimeKind.Unspecified), Price = 9.5m },
                new Screening { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = new DateTime(2031, 3, 15, 14, 0, 0, DateTimeKind.Unspecified), Price = 9.5m });
            await db.SaveChangesAsync();
        }

        using var client = _api.CreateVisitorClient();

        // Act: without any token, the schedule is public (US 1.2)
        var response = await client.GetAsync("/api/schedule?date=2031-03-14");

        // Assert: the status, then the JSON names and formats the front relies on
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var schedule = await response.Content.ReadFromJsonAsync<JsonElement>();
        schedule.GetProperty("date").GetString().ShouldBe("2031-03-14");
        var movies = schedule.GetProperty("movies");
        movies.GetArrayLength().ShouldBe(1);
        movies[0].GetProperty("title").GetString().ShouldBe("Schedule contract");
        var screenings = movies[0].GetProperty("screenings");
        screenings.GetArrayLength().ShouldBe(1);
        screenings[0].GetProperty("roomName").GetString().ShouldBe(room.Name);
        // Local time of the cinema, without any offset: the front displays it as it is
        screenings[0].GetProperty("startsAt").GetString().ShouldBe("2031-03-14T20:30:00");
        screenings[0].GetProperty("price").GetDecimal().ShouldBe(9.5m);
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_date_does_not_exist_Then_the_request_is_refused_with_400()
    {
        // Arrange
        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.GetAsync("/api/schedule?date=2026-02-30");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("US", "2.2")]
    [Trait("Category", "Integration")]
    public async Task When_seats_are_sold_or_held_Then_each_screening_shows_its_remaining_seats()
    {
        // Arrange: a room of 4 seats, on a day no other test uses. One seat is sold, one is held by a payment in progress,
        // one was held by a payment abandoned more than 10 minutes ago, one belongs to a cancelled reservation.
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = 4 };
        var seats = Enumerable.Range(1, 4).Select(number => new Seat { Id = Guid.NewGuid(), Room = room, Row = "A", Number = number }).ToList();
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Remaining seats contract", Synopsis = "Synopsis", DurationMinutes = 110, Genre = "Drama", ReleaseDate = new DateOnly(2030, 1, 1), PosterUrl = "https://example.com/poster.jpg" };
        var screening = new Screening { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = new DateTime(2031, 4, 10, 20, 30, 0, DateTimeKind.Unspecified), Price = 9.5m };
        var now = DateTime.UtcNow;
        Reservation Holding(Seat seat, ReservationStatus status, DateTime? expiresAt) => new()
        {
            Id = Guid.NewGuid(),
            Screening = screening,
            Status = status,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            TotalAmount = 9.5m,
            ReservedSeats = { new ReservedSeat { Seat = seat, ScreeningId = screening.Id } }
        };
        await using (var db = _api.CreateDbContext())
        {
            db.Seats.AddRange(seats);
            db.Reservations.AddRange(
                Holding(seats[0], ReservationStatus.Confirmed, null),
                Holding(seats[1], ReservationStatus.PendingPayment, now.AddMinutes(10)),
                Holding(seats[2], ReservationStatus.PendingPayment, now.AddMinutes(-1)),
                Holding(seats[3], ReservationStatus.Cancelled, null));
            await db.SaveChangesAsync();
        }

        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.GetAsync("/api/schedule?date=2031-04-10");

        // Assert: 4 seats, minus the sold one and the held one; the expired hold and the cancelled reservation free theirs
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var schedule = await response.Content.ReadFromJsonAsync<JsonElement>();
        var screeningJson = schedule.GetProperty("movies")[0].GetProperty("screenings")[0];
        screeningJson.GetProperty("remainingSeats").GetInt32().ShouldBe(2);
    }
}
