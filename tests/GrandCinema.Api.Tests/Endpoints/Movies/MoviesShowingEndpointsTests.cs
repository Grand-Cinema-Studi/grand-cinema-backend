using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GrandCinema.Domain.Entities;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Movies;

[Collection(ApiCollection.Name)]
public class MoviesShowingEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public MoviesShowingEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_movies_have_past_and_upcoming_screenings_Then_only_those_with_an_upcoming_screening_are_listed()
    {
        // Arrange: around the real date, which the API uses to decide what is upcoming
        var today = DateOnly.FromDateTime(DateTime.Now);
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = 120 };
        Movie NewMovie(string title) => new()
        {
            Id = Guid.NewGuid(),
            Title = $"{title} {Guid.NewGuid():N}",
            Synopsis = "Synopsis",
            DurationMinutes = 100,
            Genre = "Drama",
            ReleaseDate = new DateOnly(2025, 1, 1),
            PosterUrl = "https://example.com/poster.jpg"
        };
        var upcoming = NewMovie("Upcoming");
        var ended = NewMovie("Ended");
        await using (var db = _api.CreateDbContext())
        {
            db.Screenings.AddRange(
                new Screening { Id = Guid.NewGuid(), Movie = ended, Room = room, StartsAt = today.AddDays(-1).ToDateTime(new TimeOnly(20, 0)), Price = 9m },
                new Screening { Id = Guid.NewGuid(), Movie = upcoming, Room = room, StartsAt = today.AddDays(1).ToDateTime(new TimeOnly(20, 0)), Price = 9m });
            await db.SaveChangesAsync();
        }

        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.GetAsync("/api/movies");

        // Assert: other tests add movies too, so only these two are checked
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var titles = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .Select(m => m.GetProperty("title").GetString())
            .ToList();
        titles.ShouldContain(upcoming.Title);
        titles.ShouldNotContain(ended.Title);
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_visitor_asks_for_the_featured_movies_Then_at_most_four_movies_the_nearest_screening_first()
    {
        // Arrange: a screening in 30 minutes, local time of the cinema
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = 50 };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Featured contract", Synopsis = "Synopsis", DurationMinutes = 95, Genre = "Drama", ReleaseDate = new DateOnly(2030, 1, 1), PosterUrl = "https://example.com/poster.jpg" };
        var soon = DateTime.SpecifyKind(DateTime.Now.AddMinutes(30), DateTimeKind.Unspecified);
        await using (var db = _api.CreateDbContext())
        {
            db.Screenings.Add(new Screening { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = soon, Price = 9m });
            await db.SaveChangesAsync();
        }

        using var client = _api.CreateVisitorClient();

        // Act: without any token, as the home page
        var response = await client.GetAsync("/api/movies/featured");

        // Assert: the movie of the nearest screening first, with what the carousel shows
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var movies = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        movies.Count.ShouldBeLessThanOrEqualTo(4);
        movies.ShouldNotBeEmpty();
        var first = movies[0];
        first.GetProperty("movieId").GetString().ShouldBe(movie.Id.ToString());
        first.GetProperty("title").GetString().ShouldBe("Featured contract");
        first.GetProperty("posterUrl").GetString().ShouldBe("https://example.com/poster.jpg");
        first.GetProperty("durationMinutes").GetInt32().ShouldBe(95);
    }
}
