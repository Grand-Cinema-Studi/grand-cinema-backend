using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Programming;
using GrandCinema.Application.Features.Programming.Commands;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Programming.Commands;

public class UpdateScreeningHandlerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));
    private readonly Mock<IProgrammingRepository> _programming = new();
    private readonly Mock<IScreeningReadRepository> _screenings = new();
    private readonly Screening _bird;
    private static readonly DateTime Later = new(2026, 7, 18, 21, 0, 0, DateTimeKind.Unspecified);

    public UpdateScreeningHandlerTests()
    {
        // Bird, 161 minutes, at 20:30 in Salle 1
        var roomId = Guid.NewGuid();
        _bird = new Screening
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            StartsAt = new DateTime(2026, 7, 18, 20, 30, 0, DateTimeKind.Unspecified),
            Price = 9.5m,
            Movie = new Movie { Id = Guid.NewGuid(), Title = "Bird", Synopsis = "Charlie Parker", DurationMinutes = 161, Genre = "Drama", ReleaseDate = new DateOnly(1988, 9, 30), PosterUrl = "https://example.com/poster.jpg" }
        };
        _programming.Setup(p => p.GetScreeningAsync(_bird.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_bird);
        // By default, Bird is alone in its room and no seat is taken
        _programming
            .Setup(p => p.GetScreeningsOfRoomAsync(roomId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening> { _bird });
        _screenings
            .Setup(r => r.CountTakenSeatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
    }

    private UpdateScreeningHandler CreateHandler() => new(_programming.Object, _screenings.Object, _time);

    private void SeatsAreTaken(int count) =>
        _screenings
            .Setup(r => r.CountTakenSeatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [_bird.Id] = count });

    private void NothingWasChanged() =>
        _programming.Verify(p => p.UpdateScreeningAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    [Trait("US", "5.2")]
    public async Task When_seats_are_taken_and_the_manager_has_not_confirmed_Then_the_manager_is_warned_and_nothing_changes()
    {
        // Arrange
        SeatsAreTaken(3);

        // Act
        var result = await CreateHandler().Handle(new UpdateScreeningCommand(_bird.Id, Later, 11m, Confirmed: false));

        // Assert: criterion 3
        result.Error.ShouldBe(ProgrammingErrors.ScreeningHasReservations);
        NothingWasChanged();
    }

    [Fact]
    [Trait("US", "5.2")]
    public async Task When_the_manager_confirms_Then_the_screening_is_moved_and_its_price_changed()
    {
        // Arrange: 21:00 overlaps the current slot of Bird, which does not count
        SeatsAreTaken(3);

        // Act
        var result = await CreateHandler().Handle(new UpdateScreeningCommand(_bird.Id, Later, 11m, Confirmed: true));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _programming.Verify(p => p.UpdateScreeningAsync(_bird.Id, Later, 11m, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("US", "5.2")]
    public async Task When_the_screening_has_started_Then_screening_started_and_nothing_changes()
    {
        // Arrange: 09:30, before 10:00
        _bird.StartsAt = new DateTime(2026, 7, 18, 9, 30, 0, DateTimeKind.Unspecified);

        // Act
        var result = await CreateHandler().Handle(new UpdateScreeningCommand(_bird.Id, Later, 11m, Confirmed: true));

        // Assert
        result.Error.ShouldBe(ProgrammingErrors.ScreeningStarted);
        NothingWasChanged();
    }

    [Fact]
    [Trait("US", "5.2")]
    public async Task When_the_new_time_overlaps_another_screening_of_the_room_Then_room_not_available()
    {
        // Arrange: Metropolis at 23:30; Bird at 21:00 would end at 23:41, cleaned until 23:56
        var metropolis = new Screening
        {
            Id = Guid.NewGuid(),
            RoomId = _bird.RoomId,
            StartsAt = new DateTime(2026, 7, 18, 23, 30, 0, DateTimeKind.Unspecified),
            Price = 9m,
            Movie = new Movie { Id = Guid.NewGuid(), Title = "Metropolis", Synopsis = "Fritz Lang", DurationMinutes = 153, Genre = "Science-fiction", ReleaseDate = new DateOnly(1927, 1, 10), PosterUrl = "https://example.com/poster.jpg" }
        };
        _programming
            .Setup(p => p.GetScreeningsOfRoomAsync(_bird.RoomId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening> { _bird, metropolis });

        // Act
        var result = await CreateHandler().Handle(new UpdateScreeningCommand(_bird.Id, Later, 11m, Confirmed: false));

        // Assert
        result.Error.ShouldBe(ProgrammingErrors.RoomNotAvailable);
        NothingWasChanged();
    }
}
