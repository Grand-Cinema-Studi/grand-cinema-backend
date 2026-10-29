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

    public GetScheduleHandlerTests()
    {
        // By default no seat is taken: the US 1.1 tests do not depend on reservations
        _screenings
            .Setup(r => r.CountTakenSeatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
    }

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_screenings_exist_Then_they_are_grouped_by_movie()
    {
        // Arrange
        var day = new DateOnly(2026, 7, 18);
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Blue Note", Synopsis = "A documentary about the jazz musician", DurationMinutes = 136, Genre = "Documentary", ReleaseDate = new DateOnly(2023, 10, 1), PosterUrl = "https://example.com/poster.jpg" };
        var startsAt = day.ToDateTime(new TimeOnly(18, 30));
        _screenings
            .Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening> { new() { Id = Guid.NewGuid(), Room = room, Movie = movie, StartsAt = startsAt, Price = 9m } });
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

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_no_date_is_chosen_Then_today_is_used()
    {
        // Arrange
        var today = new DateOnly(2026, 7, 18);
        _time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));
        _screenings
            .Setup(r => r.GetScreeningsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening>());
        var handler = new GetScheduleHandler(_screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetScheduleQuery(null));

        // Assert
        _screenings.Verify(r => r.GetScreeningsAsync(today, It.IsAny<CancellationToken>()), Times.Once);
        result.Date.ShouldBe(today);
    }

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_a_movie_has_several_screenings_Then_they_are_sorted_by_time()
    {
        // Arrange
        var day = new DateOnly(2026, 7, 18);
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Blue Note", Synopsis = "A documentary about the jazz musician", DurationMinutes = 136, Genre = "Documentary", ReleaseDate = new DateOnly(2023, 10, 1), PosterUrl = "https://example.com/poster.jpg" };
        var evening = day.ToDateTime(new TimeOnly(18, 30));
        var night = day.ToDateTime(new TimeOnly(21, 0));
        _screenings
            .Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening>
            {
                new() { Id = Guid.NewGuid(), Room = room, Movie = movie, StartsAt = night, Price = 9m },
                new() { Id = Guid.NewGuid(), Room = room, Movie = movie, StartsAt = evening, Price = 9m }
            });
        var handler = new GetScheduleHandler(_screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetScheduleQuery(day));

        // Assert
        var movieSchedule = result.Movies.ShouldHaveSingleItem();
        movieSchedule.Screenings.Select(s => s.StartsAt).ShouldBe(new[] { evening, night });
    }

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_several_movies_exist_Then_they_are_sorted_by_title()
    {
        // Arrange
        var day = new DateOnly(2026, 7, 18);
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
        var startsAt = day.ToDateTime(new TimeOnly(20, 0));
        _screenings
            .Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening>
            {
                new() { Id = Guid.NewGuid(), Room = room, Movie = NewMovie("Zorro"), StartsAt = startsAt, Price = 9m },
                new() { Id = Guid.NewGuid(), Room = room, Movie = NewMovie("Blue Note"), StartsAt = startsAt, Price = 9m },
                new() { Id = Guid.NewGuid(), Room = room, Movie = NewMovie("Metropolis"), StartsAt = startsAt, Price = 9m }
            });
        var handler = new GetScheduleHandler(_screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetScheduleQuery(day));

        // Assert
        result.Movies.Select(m => m.Title).ShouldBe(new[] { "Blue Note", "Metropolis", "Zorro" });
    }

    private static Screening ScreeningOn(DateOnly day, int capacity) => new()
    {
        Id = Guid.NewGuid(),
        Room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = capacity },
        Movie = new Movie { Id = Guid.NewGuid(), Title = "Blue Note", Synopsis = "A documentary about the jazz label", DurationMinutes = 136, Genre = "Documentary", ReleaseDate = new DateOnly(2023, 10, 1), PosterUrl = "https://example.com/poster.jpg" },
        StartsAt = day.ToDateTime(new TimeOnly(20, 30)),
        Price = 9m
    };

    [Fact]
    [Trait("US", "2.2")]
    public async Task When_screenings_are_found_Then_their_taken_seats_are_counted_at_the_current_instant()
    {
        // Arrange
        var day = new DateOnly(2026, 7, 18);
        var screening = ScreeningOn(day, 170);
        _screenings
            .Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening> { screening });
        var handler = new GetScheduleHandler(_screenings.Object, _time);

        // Act
        await handler.Handle(new GetScheduleQuery(day));

        // Assert: a hold expires at an instant in UTC, so it is compared with the current instant in UTC, not with the local time
        _screenings.Verify(r => r.CountTakenSeatsAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { screening.Id })),
            new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("US", "2.2")]
    public async Task When_seats_are_taken_Then_the_remaining_seats_are_the_capacity_minus_the_taken_seats()
    {
        // Arrange
        var day = new DateOnly(2026, 7, 18);
        var screening = ScreeningOn(day, 170);
        _screenings
            .Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening> { screening });
        _screenings
            .Setup(r => r.CountTakenSeatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [screening.Id] = 120 });
        var handler = new GetScheduleHandler(_screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetScheduleQuery(day));

        // Assert
        result.Movies.ShouldHaveSingleItem().Screenings.ShouldHaveSingleItem().RemainingSeats.ShouldBe(50);
    }

    [Fact]
    [Trait("US", "2.2")]
    public async Task When_no_seat_is_taken_for_a_screening_Then_all_its_seats_remain()
    {
        // Arrange: the repository leaves out the screenings without any seat taken (default setup: empty result)
        var day = new DateOnly(2026, 7, 18);
        var screening = ScreeningOn(day, 70);
        _screenings
            .Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening> { screening });
        var handler = new GetScheduleHandler(_screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetScheduleQuery(day));

        // Assert
        result.Movies.ShouldHaveSingleItem().Screenings.ShouldHaveSingleItem().RemainingSeats.ShouldBe(70);
    }
}
