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
        var now = new DateTime(2026, 7, 18, 12, 0, 0);
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        Movie NewMovie(string title) => new()
        {
            Id = Guid.NewGuid(), Title = title, Synopsis = "Synopsis", DurationMinutes = 100,
            Genre = "Drama", ReleaseDate = new DateOnly(2025, 1, 1), PosterUrl = "https://example.com/p.jpg"
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
                At(endedThisMorning, new DateTime(2026, 7, 18, 11, 0, 0)),
                At(startsNow, new DateTime(2026, 7, 18, 12, 0, 0)),
                At(twiceThisWeek, new DateTime(2026, 7, 19, 18, 0, 0)),
                At(twiceThisWeek, new DateTime(2026, 7, 20, 18, 0, 0)));
            await seed.SaveChangesAsync();
        }

        await using var db = _database.CreateDbContext();
        var repository = new MovieReadRepository(db);

        // Act
        var result = await repository.GetMoviesScheduledFromAsync(now);

        // Assert
        result.Select(m => m.Title).ShouldBe(new[] { "Starts now", "Twice this week" }, ignoreOrder: true);
    }
}
