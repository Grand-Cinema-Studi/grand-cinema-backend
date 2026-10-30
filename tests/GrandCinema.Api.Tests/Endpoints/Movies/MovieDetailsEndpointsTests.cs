using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Movies;

[Collection(ApiCollection.Name)]
public class MovieDetailsEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public MovieDetailsEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_visitor_opens_a_movie_Then_its_upcoming_screenings_are_returned_by_day_with_their_remaining_seats()
    {
        // Arrange: around the real date, which the API uses to decide what is upcoming.
        // One screening yesterday, two tomorrow (one seat sold out of three), one a year ahead (decision of 29/10/2026).
        var today = DateOnly.FromDateTime(DateTime.Now);
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = 3 };
        var seat = new Seat { Id = Guid.NewGuid(), Room = room, Row = "A", Number = 1 };
        var movie = new Movie { Id = Guid.NewGuid(), Title = $"Movie details contract {Guid.NewGuid():N}", Synopsis = "Synopsis", DurationMinutes = 107, Genre = "Drama", ReleaseDate = new DateOnly(2014, 1, 1), PosterUrl = "https://example.com/poster.jpg" };
        Screening At(DateOnly day, int hour) => new() { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = day.ToDateTime(new TimeOnly(hour, 0)), Price = 9.5m };
        var yesterday = At(today.AddDays(-1), 20);
        var tomorrowNight = At(today.AddDays(1), 21);
        var tomorrowAfternoon = At(today.AddDays(1), 14);
        var nextYear = At(today.AddYears(1), 20);
        await using (var db = _api.CreateDbContext())
        {
            db.Screenings.AddRange(yesterday, tomorrowNight, tomorrowAfternoon, nextYear);
            db.Reservations.Add(new Reservation
            {
                Id = Guid.NewGuid(),
                Screening = tomorrowNight,
                Status = ReservationStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = 9.5m,
                ReservedSeats = { new ReservedSeat { Seat = seat, ScreeningId = tomorrowNight.Id } }
            });
            await db.SaveChangesAsync();
        }

        using var client = _api.CreateVisitorClient();

        // Act: without any token, the movie page is public (US 1.2)
        var response = await client.GetAsync($"/api/movies/{movie.Id}");

        // Assert: the JSON names and formats the front relies on
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<JsonElement>();
        details.GetProperty("title").GetString().ShouldBe(movie.Title);
        var days = details.GetProperty("days");
        days.EnumerateArray().Select(day => day.GetProperty("date").GetString())
            .ShouldBe(new[] { $"{today.AddDays(1):yyyy-MM-dd}", $"{today.AddYears(1):yyyy-MM-dd}" });
        var tomorrow = days[0].GetProperty("screenings");
        tomorrow.EnumerateArray().Select(screening => screening.GetProperty("startsAt").GetString())
            .ShouldBe(new[] { $"{today.AddDays(1):yyyy-MM-dd}T14:00:00", $"{today.AddDays(1):yyyy-MM-dd}T21:00:00" });
        tomorrow[1].GetProperty("remainingSeats").GetInt32().ShouldBe(2);
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_movie_does_not_exist_Then_404_movie_not_found()
    {
        // Arrange
        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.GetAsync($"/api/movies/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/movie-not-found");
    }
}
