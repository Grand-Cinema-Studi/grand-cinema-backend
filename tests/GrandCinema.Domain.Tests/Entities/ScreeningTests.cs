using GrandCinema.Domain.Entities;
using Shouldly;

namespace GrandCinema.Domain.Tests.Entities;

public class ScreeningTests
{
    // Bird, 161 minutes, at 14:00: the movie ends at 16:41, the room is cleaned until 16:56
    private readonly Screening _bird = new()
    {
        Id = Guid.NewGuid(),
        StartsAt = new DateTime(2026, 7, 18, 14, 0, 0, DateTimeKind.Unspecified),
        Price = 9.5m,
        Movie = new Movie { Id = Guid.NewGuid(), Title = "Bird", Synopsis = "Charlie Parker", DurationMinutes = 161, Genre = "Drama", ReleaseDate = new DateOnly(1988, 9, 30), PosterUrl = "https://example.com/poster.jpg" }
    };

    private static DateTime At(int hour, int minute) => new(2026, 7, 18, hour, minute, 0, DateTimeKind.Unspecified);

    [Fact]
    [Trait("US", "5.2")]
    public void When_a_screening_starts_once_the_room_is_cleaned_Then_there_is_no_overlap()
    {
        // Act and assert: right after the cleaning of Bird, and a movie of 120 minutes whose cleaning ends at 14:00
        _bird.OverlapsWith(At(16, 56), 100).ShouldBeFalse();
        _bird.OverlapsWith(At(11, 45), 120).ShouldBeFalse();
    }

    [Fact]
    [Trait("US", "5.2")]
    public void When_a_screening_starts_during_the_movie_or_the_cleaning_Then_it_overlaps()
    {
        // Act and assert: during the cleaning of Bird, during its movie, and a screening whose own cleaning is not over at 14:00
        _bird.OverlapsWith(At(16, 50), 90).ShouldBeTrue();
        _bird.OverlapsWith(At(15, 0), 90).ShouldBeTrue();
        _bird.OverlapsWith(At(12, 0), 110).ShouldBeTrue();
    }
}
