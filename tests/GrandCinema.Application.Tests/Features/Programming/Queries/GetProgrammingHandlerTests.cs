using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Programming;
using GrandCinema.Application.Features.Programming.Queries;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Programming.Queries;

public class GetProgrammingHandlerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));
    private readonly Mock<IProgrammingRepository> _programming = new();
    private readonly Mock<IScreeningReadRepository> _screenings = new();

    [Fact]
    [Trait("US", "5.2")]
    public async Task When_the_manager_opens_a_day_Then_every_screening_of_the_day_with_its_taken_seats_and_the_catalog()
    {
        // Arrange: Bird at 20:30 in Salle 1 (3 seats taken), Metropolis at 09:00 in Salle 2, already started
        var day = new DateOnly(2026, 7, 18);
        var salle1 = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        var salle2 = new Room { Id = Guid.NewGuid(), Name = "Salle 2", Capacity = 300 };
        var bird = new Movie { Id = Guid.NewGuid(), Title = "Bird", Synopsis = "Charlie Parker", DurationMinutes = 161, Genre = "Drama", ReleaseDate = new DateOnly(1988, 9, 30), PosterUrl = "https://example.com/bird.jpg" };
        var metropolis = new Movie { Id = Guid.NewGuid(), Title = "Metropolis", Synopsis = "Fritz Lang", DurationMinutes = 153, Genre = "Science-fiction", ReleaseDate = new DateOnly(1927, 1, 10), PosterUrl = "https://example.com/metropolis.jpg" };
        var evening = new Screening { Id = Guid.NewGuid(), Movie = bird, MovieId = bird.Id, Room = salle1, RoomId = salle1.Id, StartsAt = day.ToDateTime(new TimeOnly(20, 30)), Price = 9.5m };
        var morning = new Screening { Id = Guid.NewGuid(), Movie = metropolis, MovieId = metropolis.Id, Room = salle2, RoomId = salle2.Id, StartsAt = day.ToDateTime(new TimeOnly(9, 0)), Price = 8m };
        _screenings.Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Screening> { evening, morning });
        _screenings
            .Setup(r => r.CountTakenSeatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [evening.Id] = 3 });
        _programming.Setup(p => p.GetMoviesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Movie> { bird, metropolis });
        _programming.Setup(p => p.GetRoomsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Room> { salle1, salle2 });
        var handler = new GetProgrammingHandler(_programming.Object, _screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetProgrammingQuery(day));

        // Assert: by time, started screenings included (unlike the public schedule)
        result.Date.ShouldBe(day);
        result.Screenings.Select(s => s.MovieTitle).ShouldBe(new[] { "Metropolis", "Bird" });
        var birdScreening = result.Screenings[1];
        birdScreening.ScreeningId.ShouldBe(evening.Id);
        birdScreening.MovieId.ShouldBe(bird.Id);
        birdScreening.RoomId.ShouldBe(salle1.Id);
        birdScreening.RoomName.ShouldBe("Salle 1");
        birdScreening.StartsAt.ShouldBe(evening.StartsAt);
        birdScreening.Price.ShouldBe(9.5m);
        birdScreening.TakenSeats.ShouldBe(3);
        birdScreening.Capacity.ShouldBe(170);
        result.Screenings[0].TakenSeats.ShouldBe(0);
        result.Movies.Select(m => (m.Title, m.DurationMinutes)).ShouldBe(new[] { ("Bird", 161), ("Metropolis", 153) });
        result.Rooms.Select(r => (r.Name, r.Capacity)).ShouldBe(new[] { ("Salle 1", 170), ("Salle 2", 300) });
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_the_manager_opens_a_day_Then_the_genres_to_choose_from_are_offered()
    {
        // Arrange: an empty day, an empty catalog
        var day = new DateOnly(2026, 7, 18);
        _screenings.Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Screening>());
        _screenings
            .Setup(r => r.CountTakenSeatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
        _programming.Setup(p => p.GetMoviesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Movie>());
        _programming.Setup(p => p.GetRoomsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Room>());
        var handler = new GetProgrammingHandler(_programming.Object, _screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetProgrammingQuery(day));

        // Assert: the whole list, in alphabetical order, the genres of the demonstration catalog included
        result.Genres.ShouldBe(MovieGenres.All);
        result.Genres.Count.ShouldBe(17);
        result.Genres.ShouldContain("Comédie musicale");
        result.Genres.ShouldBeInOrder(SortDirection.Ascending, StringComparer.Ordinal);
    }
}
