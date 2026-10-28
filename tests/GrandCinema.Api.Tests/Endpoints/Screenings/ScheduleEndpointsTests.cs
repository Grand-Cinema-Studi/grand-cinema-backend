using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GrandCinema.Domain.Entities;
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
}
