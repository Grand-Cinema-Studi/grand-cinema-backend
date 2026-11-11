using GrandCinema.Domain.Entities;
using GrandCinema.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Persistence.Repositories;

public class MovieReadRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _database;

    public MovieReadRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_movies_have_past_and_upcoming_screenings_Then_only_those_with_an_upcoming_screening_are_returned()
    {
        // Arrange
        var now = new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Unspecified);
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        Movie NewMovie(string title) => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Synopsis = "Synopsis",
            DurationMinutes = 100,
            Genre = "Drama",
            ReleaseDate = new DateOnly(2025, 1, 1),
            PosterUrl = "https://example.com/p.jpg"
        };
        var endedThisMorning = NewMovie("Ended this morning");
        var startsNow = NewMovie("Starts now");
        var twiceThisWeek = NewMovie("Twice this week");
        var neverScheduled = NewMovie("Never scheduled");
        Screening At(Movie movie, DateTime startsAt) => new() { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9m };

        await using (var seed = _database.CreateDbContext())
        {
            seed.Movies.Add(neverScheduled);
            seed.Screenings.AddRange(
                At(endedThisMorning, new DateTime(2026, 7, 18, 11, 0, 0, DateTimeKind.Unspecified)),
                At(startsNow, new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Unspecified)),
                At(twiceThisWeek, new DateTime(2026, 7, 19, 18, 0, 0, DateTimeKind.Unspecified)),
                At(twiceThisWeek, new DateTime(2026, 7, 20, 18, 0, 0, DateTimeKind.Unspecified)));
            await seed.SaveChangesAsync();
        }

        await using var db = _database.CreateDbContext();
        var repository = new MovieReadRepository(db);

        // Act
        var result = await repository.GetMoviesScheduledFromAsync(now);

        // Assert
        result.Select(m => m.Title).ShouldBe(new[] { "Starts now", "Twice this week" }, ignoreOrder: true);
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_movie_is_asked_for_by_its_id_Then_it_is_returned_and_an_unknown_id_gives_nothing()
    {
        // Arrange
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Blue Note", Synopsis = "A documentary about the jazz label", DurationMinutes = 85, Genre = "Documentary", ReleaseDate = new DateOnly(2018, 1, 1), PosterUrl = "https://example.com/p.jpg" };
        await using (var seed = _database.CreateDbContext())
        {
            seed.Movies.Add(movie);
            await seed.SaveChangesAsync();
        }

        await using var db = _database.CreateDbContext();
        var repository = new MovieReadRepository(db);

        // Act
        var found = await repository.GetByIdAsync(movie.Id);
        var unknown = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        found.ShouldNotBeNull();
        found.Title.ShouldBe("Blue Note");
        unknown.ShouldBeNull();
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_movies_are_featured_Then_the_nearest_next_screening_comes_first_and_started_or_cancelled_ones_do_not_count()
    {
        // Arrange: it is 12:00 on 1 January 2040
        var now = new DateTime(2040, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = 100 };
        Movie NewMovie(string title) => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Synopsis = "Synopsis",
            DurationMinutes = 100,
            Genre = "Drama",
            ReleaseDate = new DateOnly(2039, 1, 1),
            PosterUrl = "https://example.com/p.jpg"
        };
        var startedThisMorning = NewMovie("Started this morning");
        var cancelledThisAfternoon = NewMovie("Cancelled this afternoon");
        var tonight = NewMovie("Tonight");
        var tomorrow = NewMovie("Tomorrow");
        var nextMonth = NewMovie("Next month");
        Screening At(Movie movie, DateTime startsAt, DateTime? cancelledAt = null) =>
            new() { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9m, CancelledAt = cancelledAt };

        await using (var seed = _database.CreateDbContext())
        {
            seed.Screenings.AddRange(
                At(startedThisMorning, now.AddHours(-1)),
                At(cancelledThisAfternoon, now.AddHours(2), new DateTime(2039, 12, 31, 10, 0, 0, DateTimeKind.Utc)),
                At(tonight, now.AddHours(9)),
                At(tonight, now.AddDays(4)),
                At(tomorrow, now.AddDays(1)),
                At(nextMonth, now.AddMonths(1)));
            await seed.SaveChangesAsync();
        }

        // Act: two at most
        await using var db = _database.CreateDbContext();
        var movies = await new MovieReadRepository(db).GetMoviesByNextScreeningAsync(now, 2);

        // Assert: tonight, then tomorrow; next month comes third and is left out by the limit
        movies.Select(m => m.Title).ShouldBe(new[] { "Tonight", "Tomorrow" });
    }
}
