using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Abstractions.Storage;
using GrandCinema.Application.Features.Programming;
using GrandCinema.Application.Features.Programming.Commands;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Programming.Commands;

public class UpdateMovieHandlerTests
{
    // 10:00 UTC, which FakeTimeProvider also gives as the local time
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));
    private static readonly DateTime Now = new(2026, 7, 18, 10, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly Release = new(1988, 9, 30);
    // The first bytes of a WebP file
    private static readonly byte[] Webp = [0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x56];
    private readonly Mock<IProgrammingRepository> _programming = new();
    private readonly Mock<IMovieReadRepository> _movies = new();
    private readonly Mock<IPosterStorage> _posters = new();
    private readonly Movie _bird = new() { Id = Guid.NewGuid(), Title = "Bird", Synopsis = "Charlie Parker", DurationMinutes = 161, Genre = "Biopic", ReleaseDate = Release, PosterUrl = "/posters/bird.png" };

    public UpdateMovieHandlerTests()
    {
        _movies.Setup(m => m.GetByIdAsync(_bird.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_bird);
        _posters
            .Setup(p => p.SaveAsync(It.IsAny<byte[]>(), It.IsAny<PosterFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/posters/new.webp");
    }

    private UpdateMovieHandler CreateHandler() => new(_programming.Object, _movies.Object, _posters.Object, _time);

    // As typed in the form, spaces included
    private UpdateMovieCommand Edit(int durationMinutes = 161, byte[]? poster = null) =>
        new(_bird.Id, " Bird, version restaurée ", " Charlie Parker. ", durationMinutes, "Drame", Release, poster);

    private void NothingWasUpdated()
    {
        _programming.Verify(p => p.UpdateMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()), Times.Never);
        _posters.Verify(p => p.SaveAsync(It.IsAny<byte[]>(), It.IsAny<PosterFormat>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_the_card_changes_without_a_new_poster_Then_the_movie_is_updated_trimmed_and_keeps_its_poster()
    {
        // Arrange: a screening is to come, but the duration does not change
        Movie? updated = null;
        _programming
            .Setup(p => p.UpdateMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .Callback<Movie, CancellationToken>((movie, _) => updated = movie)
            .Returns(Task.CompletedTask);
        _programming.Setup(p => p.HasScreeningsNotEndedAsync(_bird.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await CreateHandler().Handle(Edit());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        updated.ShouldNotBeNull();
        updated.Id.ShouldBe(_bird.Id);
        updated.Title.ShouldBe("Bird, version restaurée");
        updated.Synopsis.ShouldBe("Charlie Parker.");
        updated.Genre.ShouldBe("Drame");
        updated.DurationMinutes.ShouldBe(161);
        updated.PosterUrl.ShouldBe("/posters/bird.png");
        _posters.Verify(p => p.SaveAsync(It.IsAny<byte[]>(), It.IsAny<PosterFormat>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_a_new_poster_is_sent_Then_it_is_saved_and_replaces_the_old_one()
    {
        // Arrange
        Movie? updated = null;
        _programming
            .Setup(p => p.UpdateMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .Callback<Movie, CancellationToken>((movie, _) => updated = movie)
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler().Handle(Edit(poster: Webp));

        // Assert: saved as a WebP, under a new name
        result.IsSuccess.ShouldBeTrue();
        _posters.Verify(p => p.SaveAsync(Webp, PosterFormat.Webp, It.IsAny<CancellationToken>()), Times.Once);
        updated.ShouldNotBeNull().PosterUrl.ShouldBe("/posters/new.webp");
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_the_movie_does_not_exist_Then_movie_not_found_and_nothing_is_updated()
    {
        // Act
        var result = await CreateHandler().Handle(Edit() with { MovieId = Guid.NewGuid() });

        // Assert
        result.Error.ShouldBe(MovieErrors.MovieNotFound);
        NothingWasUpdated();
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_the_movie_is_archived_Then_movie_archived_and_nothing_is_updated()
    {
        // Arrange
        _bird.ArchivedAt = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await CreateHandler().Handle(Edit(poster: Webp));

        // Assert
        result.Error.ShouldBe(ProgrammingErrors.MovieArchived);
        NothingWasUpdated();
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_another_movie_has_the_same_title_and_release_date_Then_movie_already_exists_and_nothing_is_updated()
    {
        // Arrange: the title is looked for trimmed, the movie itself left out
        _programming.Setup(p => p.OtherMovieExistsAsync(_bird.Id, "Bird, version restaurée", Release, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await CreateHandler().Handle(Edit(poster: Webp));

        // Assert
        result.Error.ShouldBe(ProgrammingErrors.MovieAlreadyExists);
        NothingWasUpdated();
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_the_duration_changes_while_a_screening_has_not_ended_Then_movie_has_screenings_and_nothing_is_updated()
    {
        // Arrange: a screening started after 07:19 (10:00 minus the current 161 minutes) has not ended
        _programming.Setup(p => p.HasScreeningsNotEndedAsync(_bird.Id, Now.AddMinutes(-161), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await CreateHandler().Handle(Edit(durationMinutes: 170, poster: Webp));

        // Assert
        result.Error.ShouldBe(ProgrammingErrors.MovieHasScreenings);
        NothingWasUpdated();
    }
}
