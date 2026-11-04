using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Programming;
using GrandCinema.Application.Features.Programming.Commands;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Programming.Commands;

public class CreateScreeningHandlerTests
{
    // 10:00 UTC; without any time zone given, FakeTimeProvider is also 10:00 in local time
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));
    private readonly Mock<IProgrammingRepository> _programming = new();
    private readonly Mock<IMovieReadRepository> _movies = new();
    private readonly Movie _bird = new() { Id = Guid.NewGuid(), Title = "Bird", Synopsis = "Charlie Parker", DurationMinutes = 161, Genre = "Drama", ReleaseDate = new DateOnly(1988, 9, 30), PosterUrl = "https://example.com/poster.jpg" };
    private readonly Room _room = new() { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
    private static readonly DateTime Evening = new(2026, 7, 18, 20, 30, 0, DateTimeKind.Unspecified);

    public CreateScreeningHandlerTests()
    {
        _movies.Setup(m => m.GetByIdAsync(_bird.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_bird);
        _programming.Setup(p => p.GetRoomAsync(_room.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_room);
        // By default the room is free all day
        _programming
            .Setup(p => p.GetScreeningsOfRoomAsync(_room.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening>());
    }

    private CreateScreeningHandler CreateHandler() => new(_programming.Object, _movies.Object, _time);

    private void NothingWasAdded() =>
        _programming.Verify(p => p.AddScreeningAsync(It.IsAny<Screening>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    [Trait("US", "5.2")]
    public async Task When_the_room_is_free_Then_the_screening_is_added_with_its_movie_room_time_and_price()
    {
        // Arrange
        Screening? added = null;
        _programming
            .Setup(p => p.AddScreeningAsync(It.IsAny<Screening>(), It.IsAny<CancellationToken>()))
            .Callback<Screening, CancellationToken>((screening, _) => added = screening)
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler().Handle(new CreateScreeningCommand(_bird.Id, _room.Id, Evening, 9.5m));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        added.ShouldNotBeNull();
        added.Id.ShouldBe(result.Value);
        added.MovieId.ShouldBe(_bird.Id);
        added.RoomId.ShouldBe(_room.Id);
        added.StartsAt.ShouldBe(Evening);
        added.Price.ShouldBe(9.5m);
        added.CancelledAt.ShouldBeNull();
    }

    [Fact]
    [Trait("US", "5.2")]
    public async Task When_the_time_is_past_and_the_price_is_not_positive_Then_both_fields_are_refused_and_nothing_is_added()
    {
        // Act: 09:00 is before 10:00
        var result = await CreateHandler().Handle(new CreateScreeningCommand(_bird.Id, _room.Id, new DateTime(2026, 7, 18, 9, 0, 0, DateTimeKind.Unspecified), 0m));

        // Assert
        result.Error.ShouldNotBeNull().Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(new[] { "startsAt", "price" }, ignoreOrder: true);
        NothingWasAdded();
    }

    [Fact]
    [Trait("US", "5.2")]
    public async Task When_the_movie_does_not_exist_Then_movie_not_found()
    {
        // Act
        var result = await CreateHandler().Handle(new CreateScreeningCommand(Guid.NewGuid(), _room.Id, Evening, 9.5m));

        // Assert
        result.Error.ShouldBe(MovieErrors.MovieNotFound);
        NothingWasAdded();
    }

    [Fact]
    [Trait("US", "5.2")]
    public async Task When_the_room_does_not_exist_Then_room_not_found()
    {
        // Act
        var result = await CreateHandler().Handle(new CreateScreeningCommand(_bird.Id, Guid.NewGuid(), Evening, 9.5m));

        // Assert
        result.Error.ShouldBe(ProgrammingErrors.RoomNotFound);
        NothingWasAdded();
    }

    [Fact]
    [Trait("US", "5.2")]
    public async Task When_another_screening_needs_the_room_at_that_time_Then_room_not_available()
    {
        // Arrange: Metropolis at 18:00, 153 minutes: the room is busy until 20:33, then cleaned until 20:48
        var metropolis = new Screening
        {
            Id = Guid.NewGuid(),
            RoomId = _room.Id,
            StartsAt = new DateTime(2026, 7, 18, 18, 0, 0, DateTimeKind.Unspecified),
            Price = 9m,
            Movie = new Movie { Id = Guid.NewGuid(), Title = "Metropolis", Synopsis = "Fritz Lang", DurationMinutes = 153, Genre = "Science-fiction", ReleaseDate = new DateOnly(1927, 1, 10), PosterUrl = "https://example.com/poster.jpg" }
        };
        _programming
            .Setup(p => p.GetScreeningsOfRoomAsync(_room.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening> { metropolis });

        // Act
        var result = await CreateHandler().Handle(new CreateScreeningCommand(_bird.Id, _room.Id, Evening, 9.5m));

        // Assert
        result.Error.ShouldBe(ProgrammingErrors.RoomNotAvailable);
        NothingWasAdded();
    }
}
