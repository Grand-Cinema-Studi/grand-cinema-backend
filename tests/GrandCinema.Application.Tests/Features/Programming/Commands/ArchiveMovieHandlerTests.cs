using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Programming;
using GrandCinema.Application.Features.Programming.Commands;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Programming.Commands;

public class ArchiveMovieHandlerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));
    private readonly Mock<IProgrammingRepository> _programming = new();
    private readonly Mock<IMovieReadRepository> _movies = new();
    private readonly Movie _bird = new() { Id = Guid.NewGuid(), Title = "Bird", Synopsis = "Charlie Parker", DurationMinutes = 161, Genre = "Biopic", ReleaseDate = new DateOnly(1988, 9, 30), PosterUrl = "/posters/bird.png" };

    public ArchiveMovieHandlerTests()
    {
        _movies.Setup(m => m.GetByIdAsync(_bird.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_bird);
    }

    private ArchiveMovieHandler CreateHandler() => new(_programming.Object, _movies.Object, _time);

    private void NothingWasArchived() =>
        _programming.Verify(p => p.ArchiveMovieAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_no_screening_is_to_come_Then_the_movie_is_archived_at_the_current_instant()
    {
        // Act
        var result = await CreateHandler().Handle(new ArchiveMovieCommand(_bird.Id));

        // Assert: the instant in UTC, as CancelledAt
        result.IsSuccess.ShouldBeTrue();
        _programming.Verify(p => p.ArchiveMovieAsync(_bird.Id, new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_a_screening_has_not_ended_Then_movie_has_screenings_and_the_movie_is_not_archived()
    {
        // Arrange: started after 07:19 (10:00 minus 161 minutes)
        _programming
            .Setup(p => p.HasScreeningsNotEndedAsync(_bird.Id, new DateTime(2026, 7, 18, 10, 0, 0).AddMinutes(-161), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await CreateHandler().Handle(new ArchiveMovieCommand(_bird.Id));

        // Assert
        result.Error.ShouldBe(ProgrammingErrors.MovieHasScreenings);
        NothingWasArchived();
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_the_movie_is_already_archived_Then_nothing_changes()
    {
        // Arrange
        _bird.ArchivedAt = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await CreateHandler().Handle(new ArchiveMovieCommand(_bird.Id));

        // Assert: a success, the first date kept
        result.IsSuccess.ShouldBeTrue();
        NothingWasArchived();
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_the_movie_to_archive_does_not_exist_Then_movie_not_found()
    {
        // Act
        var result = await CreateHandler().Handle(new ArchiveMovieCommand(Guid.NewGuid()));

        // Assert
        result.Error.ShouldBe(MovieErrors.MovieNotFound);
        NothingWasArchived();
    }
}
