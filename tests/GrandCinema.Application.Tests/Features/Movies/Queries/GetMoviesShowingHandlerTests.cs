using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Movies.Queries;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace GrandCinema.Application.Tests.Features.Movies.Queries;

public class GetMoviesShowingHandlerTests
{
    private readonly Mock<IMovieReadRepository> _movies = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));

    private static Movie NewMovie(string title) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Synopsis = "Synopsis",
        DurationMinutes = 100,
        Genre = "Drama",
        ReleaseDate = new DateOnly(2025, 1, 1),
        PosterUrl = "https://example.com/poster.jpg",
    };

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_movies_are_requested_Then_the_repository_is_queried_from_now()
    {
        // Arrange
        _time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));
        _movies
            .Setup(r => r.GetMoviesScheduledFromAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie>());
        var handler = new GetMoviesShowingHandler(_movies.Object, _time);

        // Act
        await handler.Handle(new GetMoviesShowingQuery());

        // Assert
        _movies.Verify(r => r.GetMoviesScheduledFromAsync(new DateTime(2026, 7, 18, 12, 0, 0), It.IsAny<CancellationToken>()), Times.Once);
    }
}
