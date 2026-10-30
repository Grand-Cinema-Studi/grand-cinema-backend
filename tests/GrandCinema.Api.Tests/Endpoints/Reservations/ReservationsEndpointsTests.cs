using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GrandCinema.Domain.Entities;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Reservations;

[Collection(ApiCollection.Name)]
public class ReservationsEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public ReservationsEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    // A screening on a day no other test uses, in its own room of seatCount seats (row A)
    private async Task<Guid> ScreeningWithSeatsAsync(int seatCount, DateTime startsAt)
    {
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = seatCount };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Reservations contract", Synopsis = "Synopsis", DurationMinutes = 110, Genre = "Drama", ReleaseDate = new DateOnly(2030, 1, 1), PosterUrl = "https://example.com/poster.jpg" };
        var screening = new Screening { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9.5m };
        await using var db = _api.CreateDbContext();
        db.Seats.AddRange(Enumerable.Range(1, seatCount).Select(number => new Seat { Id = Guid.NewGuid(), Room = room, Row = "A", Number = number }));
        db.Screenings.Add(screening);
        await db.SaveChangesAsync();
        return screening.Id;
    }

    private async Task<HttpClient> SignedInCustomerAsync()
    {
        var client = _api.CreateVisitorClient();
        var token = await GrandCinemaApiFactory.RegisterCustomerAsync(client, GrandCinemaApiFactory.NewEmail());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_the_visitor_is_not_signed_in_Then_401()
    {
        // Arrange
        var screeningId = await ScreeningWithSeatsAsync(4, new DateTime(2031, 5, 20, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 2 });

        // Assert: the front then invites the visitor to sign in or to create an account
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_a_customer_holds_two_seats_Then_201_and_the_seats_are_held_for_10_minutes()
    {
        // Arrange: 4 seats, nothing sold
        var screeningId = await ScreeningWithSeatsAsync(4, new DateTime(2031, 5, 21, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = await SignedInCustomerAsync();
        var before = DateTime.UtcNow;

        // Act
        var response = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 2 });

        // Assert: a hold waiting for payment, not a sale (criterion 3)
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var hold = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reservationId = hold.GetProperty("reservationId").GetString();
        response.Headers.Location?.ToString().ShouldBe($"/api/reservations/{reservationId}");
        hold.GetProperty("status").GetString().ShouldBe("PendingPayment");
        hold.GetProperty("seats").EnumerateArray().Select(seat => seat.GetString()).ShouldBe(new[] { "A1", "A2" });
        hold.GetProperty("totalAmount").GetDecimal().ShouldBe(19m);
        // An instant in UTC ("Z"), 10 minutes after the request
        var expiresAt = hold.GetProperty("expiresAt").GetString()!;
        expiresAt.ShouldEndWith("Z");
        DateTime.Parse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            .ShouldBeInRange(before.AddMinutes(10), DateTime.UtcNow.AddMinutes(10));
        // The counter of the schedule is updated at once (US 2.2): 4 seats, 2 held
        var schedule = await client.GetFromJsonAsync<JsonElement>("/api/schedule?date=2031-05-21");
        schedule.GetProperty("movies")[0].GetProperty("screenings")[0].GetProperty("remainingSeats").GetInt32().ShouldBe(2);
        // The owner can read the hold again (payment page, US 3.1)
        (await client.GetAsync(response.Headers.Location)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_more_seats_are_asked_than_remain_Then_409_not_enough_seats()
    {
        // Arrange
        var screeningId = await ScreeningWithSeatsAsync(2, new DateTime(2031, 5, 22, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = await SignedInCustomerAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 3 });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/not-enough-seats");
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_two_users_book_the_last_seat_at_the_same_time_Then_only_one_succeeds()
    {
        // Arrange: bug 0001, a screening with a single seat and two signed-in customers
        var screeningId = await ScreeningWithSeatsAsync(1, new DateTime(2031, 5, 23, 20, 30, 0, DateTimeKind.Unspecified));
        using var first = await SignedInCustomerAsync();
        using var second = await SignedInCustomerAsync();

        // Act: both requests leave at the same time
        var responses = await Task.WhenAll(
            first.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 1 }),
            second.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 1 }));

        // Assert: one hold and one explicit refusal, never two customers for one seat
        responses.Select(response => response.StatusCode).OrderBy(status => status)
            .ShouldBe(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
        var refused = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        var problem = await refused.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/not-enough-seats");
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_another_customer_asks_for_the_reservation_Then_404()
    {
        // Arrange: a hold of one customer
        var screeningId = await ScreeningWithSeatsAsync(2, new DateTime(2031, 5, 24, 20, 30, 0, DateTimeKind.Unspecified));
        using var owner = await SignedInCustomerAsync();
        using var other = await SignedInCustomerAsync();
        var created = await owner.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 1 });
        created.EnsureSuccessStatusCode();

        // Act
        var response = await other.GetAsync(created.Headers.Location);

        // Assert: the same answer as an unknown reservation, its existence is not revealed (OWASP A01)
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/reservation-not-found");
    }
}
