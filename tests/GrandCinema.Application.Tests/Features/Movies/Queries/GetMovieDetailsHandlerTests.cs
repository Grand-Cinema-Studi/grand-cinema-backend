using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Movies.Queries;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Movies.Queries;

public class GetMovieDetailsHandlerTests
{
    private readonly Mock<IMovieReadRepository> _movies = new();
    private readonly Mock<IScreeningReadRepository> _screenings = new();
    // 10:00 UTC on 18/07/2026, so 12:00 at the cinema (Paris, summer time)
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));
    private readonly Movie _whiplash = new()
    {
        Id = Guid.NewGuid(),
        Title = "Whiplash",
        Synopsis = "A young jazz drummer",
        DurationMinutes = 107,
        Genre = "Drama",
        ReleaseDate = new DateOnly(2014, 1, 1),
        PosterUrl = "https://example.com/poster.jpg"
    };

    public GetMovieDetailsHandlerTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));
        _movies
            .Setup(r => r.GetByIdAsync(_whiplash.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_whiplash);
        _screenings
            .Setup(r => r.GetUpcomingScreeningsOfMovieAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening>());
        // By default no seat is taken
        _screenings
            .Setup(r => r.CountTakenSeatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
    }

    private GetMovieDetailsHandler CreateHandler() => new(_movies.Object, _screenings.Object, _time);

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_the_movie_does_not_exist_Then_movie_not_found_is_returned()
    {
        // Arrange: an id the repository does not know
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMovieDetailsQuery(Guid.NewGuid()));

        // Assert: no screening is searched for a movie that does not exist
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(MovieErrors.MovieNotFound);
        _screenings.Verify(r => r.GetUpcomingScreeningsOfMovieAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_the_movie_exists_Then_its_screenings_are_searched_from_now_and_their_seats_counted_at_the_current_instant()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetMovieDetailsQuery(_whiplash.Id));

        // Assert: "upcoming" in the local time of the cinema (StartsAt); holds checked against the current instant in UTC (ExpiresAt)
        _screenings.Verify(r => r.GetUpcomingScreeningsOfMovieAsync(_whiplash.Id, new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Unspecified), It.IsAny<CancellationToken>()), Times.Once);
        _screenings.Verify(r => r.CountTakenSeatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_screenings_are_on_several_days_Then_they_are_grouped_by_day_and_sorted_with_their_remaining_seats()
    {
        // Arrange: given in disorder, one of them a year ahead, the night one sold out
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        Screening At(DateTime startsAt) => new() { Id = Guid.NewGuid(), Movie = _whiplash, Room = room, StartsAt = startsAt, Price = 9m };
        var tomorrowNight = At(new DateTime(2026, 7, 19, 21, 0, 0, DateTimeKind.Unspecified));
        var tomorrowAfternoon = At(new DateTime(2026, 7, 19, 14, 0, 0, DateTimeKind.Unspecified));
        var nextYear = At(new DateTime(2027, 7, 18, 20, 30, 0, DateTimeKind.Unspecified));
        _screenings
            .Setup(r => r.GetUpcomingScreeningsOfMovieAsync(_whiplash.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening> { nextYear, tomorrowNight, tomorrowAfternoon });
        _screenings
            .Setup(r => r.CountTakenSeatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [tomorrowNight.Id] = 170 });
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMovieDetailsQuery(_whiplash.Id));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Title.ShouldBe("Whiplash");
        var days = result.Value.Days;
        days.Select(day => day.Date).ShouldBe(new[] { new DateOnly(2026, 7, 19), new DateOnly(2027, 7, 18) });
        days[0].Screenings.Select(s => s.StartsAt).ShouldBe(new[] { tomorrowAfternoon.StartsAt, tomorrowNight.StartsAt });
        days[0].Screenings.Select(s => s.RemainingSeats).ShouldBe(new[] { 170, 0 });
    }
}
