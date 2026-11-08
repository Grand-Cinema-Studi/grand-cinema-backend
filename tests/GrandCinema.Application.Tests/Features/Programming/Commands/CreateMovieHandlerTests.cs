using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Abstractions.Storage;
using GrandCinema.Application.Features.Programming;
using GrandCinema.Application.Features.Programming.Commands;
using GrandCinema.Domain.Entities;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Programming.Commands;

public class CreateMovieHandlerTests
{
    // The first bytes of a PNG: the rules read nothing else
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
    private static readonly DateOnly Release = new(1988, 9, 30);
    private readonly Mock<IProgrammingRepository> _programming = new();
    private readonly Mock<IPosterStorage> _posters = new();

    public CreateMovieHandlerTests()
    {
        _posters
            .Setup(p => p.SaveAsync(It.IsAny<byte[]>(), It.IsAny<PosterFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/posters/new.png");
    }

    private CreateMovieHandler CreateHandler() => new(_programming.Object, _posters.Object);

    private static CreateMovieCommand Bird(byte[]? poster = null) =>
        new("  Bird ", " Charlie Parker. ", 161, " Drame ", Release, poster ?? Png);

    private void NothingWasSaved()
    {
        _posters.Verify(p => p.SaveAsync(It.IsAny<byte[]>(), It.IsAny<PosterFormat>(), It.IsAny<CancellationToken>()), Times.Never);
        _programming.Verify(p => p.AddMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_the_card_is_complete_Then_the_poster_is_saved_and_the_movie_added_with_trimmed_fields()
    {
        // Arrange
        Movie? added = null;
        _programming
            .Setup(p => p.AddMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .Callback<Movie, CancellationToken>((movie, _) => added = movie)
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler().Handle(Bird());

        // Assert: the poster is saved as a PNG, recognised from its first bytes
        result.IsSuccess.ShouldBeTrue();
        _posters.Verify(p => p.SaveAsync(Png, PosterFormat.Png, It.IsAny<CancellationToken>()), Times.Once);
        added.ShouldNotBeNull();
        added.Id.ShouldBe(result.Value);
        added.Title.ShouldBe("Bird");
        added.Synopsis.ShouldBe("Charlie Parker.");
        added.DurationMinutes.ShouldBe(161);
        added.Genre.ShouldBe("Drame");
        added.ReleaseDate.ShouldBe(Release);
        added.PosterUrl.ShouldBe("/posters/new.png");
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_nothing_is_given_Then_every_field_is_refused_and_nothing_is_saved()
    {
        // Act: blanks count as nothing
        var result = await CreateHandler().Handle(new CreateMovieCommand(" ", null, null, "", null, null));

        // Assert
        result.Error.ShouldNotBeNull().Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull().Keys
            .ShouldBe(new[] { "title", "synopsis", "durationMinutes", "genre", "releaseDate", "poster" }, ignoreOrder: true);
        NothingWasSaved();
    }

    [Theory]
    [Trait("US", "5.6")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task When_the_poster_is_too_heavy_or_not_an_image_Then_it_is_refused_and_nothing_is_saved(bool tooHeavy)
    {
        // Arrange: a PNG one byte beyond 2 MB, or a text file
        byte[] poster = tooHeavy ? [.. Png, .. new byte[PosterRules.MaxBytes + 1 - Png.Length]] : "<svg "u8.ToArray();

        // Act
        var result = await CreateHandler().Handle(Bird(poster));

        // Assert: criterion 4, only the poster is refused
        result.Error.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(new[] { "poster" });
        NothingWasSaved();
    }

    [Fact]
    [Trait("US", "5.6")]
    public async Task When_the_same_title_and_release_date_exist_Then_movie_already_exists_and_nothing_is_saved()
    {
        // Arrange: the title is looked for trimmed
        _programming.Setup(p => p.MovieExistsAsync("Bird", Release, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await CreateHandler().Handle(Bird());

        // Assert
        result.Error.ShouldBe(ProgrammingErrors.MovieAlreadyExists);
        NothingWasSaved();
    }
}
