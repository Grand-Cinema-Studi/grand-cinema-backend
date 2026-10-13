using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Screenings.Queries;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Screenings.Queries;

public class GetScheduleHandlerTests
{
    private readonly Mock<IScreeningReadRepository> _screenings = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_screenings_exist_Then_they_are_grouped_by_movie()
    {
        // Arrange
        var day = new DateOnly(2026, 7, 18);
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Blue Note", Synopsis = "A documentary about the jazz musician", DurationMinutes = 136, Genre = "Documentary", ReleaseDate = new DateTime(2023, 10, 1), PosterUrl = "https://example.com/poster.jpg" };
        var startsAt = day.ToDateTime(new TimeOnly(18, 30));
        _screenings
            .Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening> {new() { Id = Guid.NewGuid(), Room = room, Movie = movie, StartsAt = startsAt, Price = 9m }});
        var handler = new GetScheduleHandler(_screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetScheduleQuery(day));

        // Assert
        var movieShedule = result.Movies.ShouldHaveSingleItem();
        movieShedule.Title.ShouldBe("Blue Note");
        var screening = movieShedule.Screenings.ShouldHaveSingleItem();
        screening.RoomName.ShouldBe("Salle 1");
        screening.StartsAt.ShouldBe(startsAt);
    }

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_a_date_is_chosen_Then_the_repository_is_queried_for_that_day()
    {
        // Arrange
        var chosenDay = new DateOnly(2026, 7, 19);
        _screenings
            .Setup(r => r.GetScreeningsAsync(chosenDay, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening>());
        var handler = new GetScheduleHandler(_screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetScheduleQuery(chosenDay));

        // Assert
        _screenings.Verify(r => r.GetScreeningsAsync(chosenDay, It.IsAny<CancellationToken>()), Times.Once);
        result.Date.ShouldBe(chosenDay);
    }

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_no_screening_exists_Then_the_movie_list_is_empty()
    {
        // Arrange
        var day = new DateOnly(2026, 7, 18);
        _screenings
            .Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening>());
        var handler = new GetScheduleHandler(_screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetScheduleQuery(day));

        // Assert
        result.Movies.ShouldBeEmpty();
    }
}
