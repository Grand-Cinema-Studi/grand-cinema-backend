using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Programming;

[Collection(ApiCollection.Name)]
public class ProgrammingEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public ProgrammingEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    // A movie of 110 minutes and its own room of seatCount seats (row A)
    private async Task<(Guid MovieId, Guid RoomId)> MovieAndRoomAsync(int seatCount = 2)
    {
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = seatCount };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Programming contract", Synopsis = "Synopsis", DurationMinutes = 110, Genre = "Drama", ReleaseDate = new DateOnly(2030, 1, 1), PosterUrl = "https://example.com/poster.jpg" };
        await using var db = _api.CreateDbContext();
        db.Movies.Add(movie);
        db.Seats.AddRange(Enumerable.Range(1, seatCount).Select(number => new Seat { Id = Guid.NewGuid(), Room = room, Row = "A", Number = number }));
        await db.SaveChangesAsync();
        return (movie.Id, room.Id);
    }

    // Creates a screening as the front does, and returns its identifier
    private static async Task<string> CreateScreeningAsync(HttpClient manager, Guid movieId, Guid roomId, string startsAt)
    {
        var response = await manager.PostAsJsonAsync("/api/programming/screenings", new { movieId, roomId, startsAt, price = 9.5m });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("screeningId").GetString()!;
    }

    // The customer holds one seat, as the front does, and gets the address of the hold
    private static async Task<Uri> HoldOneSeatAsync(HttpClient customer, string screeningId)
    {
        var held = await customer.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 1 });
        held.StatusCode.ShouldBe(HttpStatusCode.Created);
        return held.Headers.Location!;
    }

    // The screenings of a day, as any visitor sees them (US 1.1)
    private async Task<List<JsonElement>> PublicScreeningsAsync(string date)
    {
        using var visitor = _api.CreateVisitorClient();
        var schedule = await visitor.GetFromJsonAsync<JsonElement>($"/api/schedule?date={date}");
        return schedule.GetProperty("movies").EnumerateArray()
            .SelectMany(movie => movie.GetProperty("screenings").EnumerateArray())
            .ToList();
    }

    [Fact]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    public async Task When_the_manager_creates_a_screening_Then_201_and_it_appears_in_the_public_schedule()
    {
        // Arrange
        var (movieId, roomId) = await MovieAndRoomAsync();
        using var manager = await _api.CreateClientWithRoleAsync(UserRole.Manager);

        // Act
        var screeningId = await CreateScreeningAsync(manager, movieId, roomId, "2032-03-10T20:30:00");

        // Assert: criterion 1, at the time and price the manager chose
        var screening = (await PublicScreeningsAsync("2032-03-10")).ShouldHaveSingleItem();
        screening.GetProperty("screeningId").GetString().ShouldBe(screeningId);
        screening.GetProperty("startsAt").GetString().ShouldBe("2032-03-10T20:30:00");
        screening.GetProperty("price").GetDecimal().ShouldBe(9.5m);
        screening.GetProperty("remainingSeats").GetInt32().ShouldBe(2);
    }

    [Fact]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    public async Task When_the_manager_moves_a_screening_Then_204_and_the_public_schedule_shows_its_new_time_and_price()
    {
        // Arrange
        var (movieId, roomId) = await MovieAndRoomAsync();
        using var manager = await _api.CreateClientWithRoleAsync(UserRole.Manager);
        var screeningId = await CreateScreeningAsync(manager, movieId, roomId, "2032-03-11T18:00:00");

        // Act
        var response = await manager.PutAsJsonAsync($"/api/programming/screenings/{screeningId}", new { startsAt = "2032-03-11T21:00:00", price = 11m });

        // Assert: criterion 2
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var screening = (await PublicScreeningsAsync("2032-03-11")).ShouldHaveSingleItem();
        screening.GetProperty("startsAt").GetString().ShouldBe("2032-03-11T21:00:00");
        screening.GetProperty("price").GetDecimal().ShouldBe(11m);
    }

    [Fact]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    public async Task When_the_manager_cancels_a_screening_without_any_reservation_Then_204_and_it_leaves_the_public_schedule()
    {
        // Arrange
        var (movieId, roomId) = await MovieAndRoomAsync();
        using var manager = await _api.CreateClientWithRoleAsync(UserRole.Manager);
        var screeningId = await CreateScreeningAsync(manager, movieId, roomId, "2032-03-12T20:30:00");

        // Act: no seat held or sold, so no confirmation is asked
        var response = await manager.DeleteAsync($"/api/programming/screenings/{screeningId}");

        // Assert: criterion 2
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await PublicScreeningsAsync("2032-03-12")).ShouldBeEmpty();
    }

    [Fact]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    public async Task When_seats_of_the_screening_are_held_Then_409_until_the_manager_confirms_and_the_hold_is_cancelled()
    {
        // Arrange
        var (movieId, roomId) = await MovieAndRoomAsync();
        using var manager = await _api.CreateClientWithRoleAsync(UserRole.Manager);
        var screeningId = await CreateScreeningAsync(manager, movieId, roomId, "2032-03-13T20:30:00");
        using var customer = await _api.CreateClientWithRoleAsync(UserRole.Customer);
        var hold = await HoldOneSeatAsync(customer, screeningId);

        // Act
        var warned = await manager.DeleteAsync($"/api/programming/screenings/{screeningId}");
        var stillPublic = await PublicScreeningsAsync("2032-03-13");
        var confirmed = await manager.DeleteAsync($"/api/programming/screenings/{screeningId}?confirmed=true");

        // Assert: criterion 3, the manager is warned and nothing changes until they confirm
        warned.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await warned.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString().ShouldBe("/errors/screening-has-reservations");
        stillPublic.ShouldHaveSingleItem();
        confirmed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await PublicScreeningsAsync("2032-03-13")).ShouldBeEmpty();
        // The customer still reads their hold, cancelled: a payment arriving now would be refunded (US 3.1)
        (await customer.GetFromJsonAsync<JsonElement>(hold)).GetProperty("status").GetString().ShouldBe("Cancelled");
    }

    [Theory]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.Cashier)]
    public async Task When_another_role_uses_the_programming_Then_403(UserRole role)
    {
        // Arrange
        var (movieId, roomId) = await MovieAndRoomAsync();
        using var client = await _api.CreateClientWithRoleAsync(role);

        // Act
        var read = await client.GetAsync("/api/programming?date=2032-03-14");
        var created = await client.PostAsJsonAsync("/api/programming/screenings", new { movieId, roomId, startsAt = "2032-03-14T20:30:00", price = 9.5m });

        // Assert: criterion 4, and the DoD of epic 5: the cashier reads the reservations (US 5.1), never the programming
        read.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        created.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await PublicScreeningsAsync("2032-03-14")).ShouldBeEmpty();
    }

    [Fact]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    public async Task When_the_manager_opens_a_day_Then_its_screenings_with_their_taken_seats_the_movies_and_the_rooms()
    {
        // Arrange: one seat of two held by a customer
        var (movieId, roomId) = await MovieAndRoomAsync();
        using var manager = await _api.CreateClientWithRoleAsync(UserRole.Manager);
        var screeningId = await CreateScreeningAsync(manager, movieId, roomId, "2032-03-15T20:30:00");
        using var customer = await _api.CreateClientWithRoleAsync(UserRole.Customer);
        await HoldOneSeatAsync(customer, screeningId);

        // Act
        var programming = await manager.GetFromJsonAsync<JsonElement>("/api/programming?date=2032-03-15");

        // Assert: the names and formats the front relies on
        programming.GetProperty("date").GetString().ShouldBe("2032-03-15");
        var screening = programming.GetProperty("screenings").EnumerateArray().ShouldHaveSingleItem();
        screening.GetProperty("screeningId").GetString().ShouldBe(screeningId);
        screening.GetProperty("movieId").GetString().ShouldBe(movieId.ToString());
        screening.GetProperty("movieTitle").GetString().ShouldBe("Programming contract");
        screening.GetProperty("roomId").GetString().ShouldBe(roomId.ToString());
        screening.GetProperty("roomName").GetString().ShouldStartWith("Room ");
        screening.GetProperty("startsAt").GetString().ShouldBe("2032-03-15T20:30:00");
        screening.GetProperty("price").GetDecimal().ShouldBe(9.5m);
        screening.GetProperty("takenSeats").GetInt32().ShouldBe(1);
        screening.GetProperty("capacity").GetInt32().ShouldBe(2);
        programming.GetProperty("movies").EnumerateArray().ShouldContain(movie =>
            movie.GetProperty("movieId").GetString() == movieId.ToString() && movie.GetProperty("durationMinutes").GetInt32() == 110);
        programming.GetProperty("rooms").EnumerateArray().ShouldContain(room =>
            room.GetProperty("roomId").GetString() == roomId.ToString() && room.GetProperty("capacity").GetInt32() == 2);
    }

    [Fact]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    public async Task When_the_time_is_sent_with_a_time_zone_Then_400_on_starts_at_instead_of_500()
    {
        // Arrange
        var (movieId, roomId) = await MovieAndRoomAsync();
        using var manager = await _api.CreateClientWithRoleAsync(UserRole.Manager);

        // Act: what Swagger proposes by default, an instant in UTC ("Z"), not a time of the cinema
        var response = await manager.PostAsJsonAsync("/api/programming/screenings", new { movieId, roomId, startsAt = "2032-03-16T20:30:00Z", price = 9.5m });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty("startsAt", out _).ShouldBeTrue();
        (await PublicScreeningsAsync("2032-03-16")).ShouldBeEmpty();
    }
}
