using GrandCinema.Domain.Entities;
using GrandCinema.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Persistence.Repositories;

public class ScreeningReadRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _database;

    public ScreeningReadRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_screenings_exist_on_several_days_Then_only_those_of_the_chosen_day_are_returned()
    {
        // Arrange
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Blue Note", Synopsis = "A documentary about the jazz label", DurationMinutes = 136, Genre = "Documentary", ReleaseDate = new DateOnly(2023, 10, 1), PosterUrl = "https://example.com/poster.jpg" };
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        Screening At(DateTime startsAt) => new() { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9m };

        await using (var seed = _database.CreateDbContext())
        {
            seed.Screenings.AddRange(
                At(new DateTime(2026, 7, 18, 23, 30, 0)),
                At(new DateTime(2026, 7, 19, 0, 0, 0)),
                At(new DateTime(2026, 7, 19, 21, 0, 0)),
                At(new DateTime(2026, 7, 20, 0, 0, 0)));
            await seed.SaveChangesAsync();
        }

        await using var db = _database.CreateDbContext();
        var repository = new ScreeningReadRepository(db);

        // Act
        var result = await repository.GetScreeningsAsync(new DateOnly(2026, 7, 19));

        // Assert
        result.Select(s => s.StartsAt).ShouldBe(new[] { new DateTime(2026, 7, 19, 0, 0, 0), new DateTime(2026, 7, 19, 21, 0, 0) }, ignoreOrder: true);
        result.ShouldAllBe(s => s.Movie.Title == "Blue Note" && s.Room.Name == "Salle 1");
    }
}
