using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Movies.Queries;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

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
        var query = new GetMoviesShowingQuery();
        await handler.Handle(query);

        // Assert
        _movies.Verify(r => r.GetMoviesScheduledFromAsync(new DateTime(2026, 7, 18, 12, 0, 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_a_movie_is_scheduled_Then_it_is_returned()
    {
        // Arrange
        var movie = NewMovie("Blue Note");
        _movies
            .Setup(r=> r.GetMoviesScheduledFromAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { movie });
        var handler = new GetMoviesShowingHandler(_movies.Object, _time);

        // Act
        var result = await handler.Handle(new GetMoviesShowingQuery());

        // Assert
        var dto = result.ShouldHaveSingleItem();
        dto.MovieId.ShouldBe(movie.Id);
        dto.Title.ShouldContain("Blue Note");
    }

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_several_movies_are_scheduled_Then_they_are_sorted_by_title()
    {
        // Arrange
        _movies
            .Setup(r => r.GetMoviesScheduledFromAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { NewMovie("Zorro"), NewMovie("Blue Note"), NewMovie("Metropolis") });
        var handler = new GetMoviesShowingHandler(_movies.Object, _time);

        // Act
        var result = await handler.Handle(new GetMoviesShowingQuery());

        // Assert
        result.Select(m => m.Title).ShouldBe(new[] { "Blue Note", "Metropolis", "Zorro" });
    }
}
