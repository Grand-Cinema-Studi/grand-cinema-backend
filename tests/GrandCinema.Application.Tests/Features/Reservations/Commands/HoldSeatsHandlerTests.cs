using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations;
using GrandCinema.Application.Features.Reservations.Commands;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Reservations.Commands;

public class HoldSeatsHandlerTests
{
    // 16:00 UTC on 18/07/2026, so 18:00 at the cinema (Paris, summer time)
    private static readonly DateTime NowUtc = new(2026, 7, 18, 16, 0, 0, DateTimeKind.Utc);
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(NowUtc));
    private readonly Mock<IScreeningReadRepository> _screenings = new();
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Screening _evening = new() { Id = Guid.NewGuid(), MovieId = Guid.NewGuid(), RoomId = Guid.NewGuid(), StartsAt = new DateTime(2026, 7, 18, 20, 30, 0, DateTimeKind.Unspecified), Price = 9.5m };

    public HoldSeatsHandlerTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));
        _screenings
            .Setup(r => r.GetByIdAsync(_evening.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_evening);
    }

    private HoldSeatsHandler CreateHandler() => new(_screenings.Object, _reservations.Object, _time);

    private void NoHoldWasAttempted() =>
        _reservations.Verify(r => r.TryHoldSeatsAsync(It.IsAny<Guid>(), It.IsAny<Screening>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [Trait("US", "2.3")]
    public async Task When_the_number_of_seats_is_not_between_1_and_10_Then_the_request_is_refused_before_any_hold(int seatCount)
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new HoldSeatsCommand(_userId, _evening.Id, seatCount));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull().Keys.ShouldContain("seatCount");
        NoHoldWasAttempted();
    }

    [Fact]
    [Trait("US", "2.3")]
    public async Task When_the_screening_does_not_exist_Then_screening_not_found()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new HoldSeatsCommand(_userId, Guid.NewGuid(), 2));

        // Assert
        result.Error.ShouldBe(ReservationErrors.ScreeningNotFound);
        NoHoldWasAttempted();
    }

    [Fact]
    [Trait("US", "2.3")]
    public async Task When_the_screening_starts_now_Then_screening_started()
    {
        // Arrange: a screening at 18:00, the local time of the cinema
        var startingNow = new Screening { Id = Guid.NewGuid(), RoomId = Guid.NewGuid(), StartsAt = new DateTime(2026, 7, 18, 18, 0, 0, DateTimeKind.Unspecified), Price = 9.5m };
        _screenings.Setup(r => r.GetByIdAsync(startingNow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(startingNow);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new HoldSeatsCommand(_userId, startingNow.Id, 2));

        // Assert
        result.Error.ShouldBe(ReservationErrors.ScreeningStarted);
        NoHoldWasAttempted();
    }

    [Fact]
    [Trait("US", "2.3")]
    public async Task When_not_enough_seats_are_free_Then_not_enough_seats()
    {
        // Arrange: the repository finds fewer free seats than asked
        _reservations
            .Setup(r => r.TryHoldSeatsAsync(_userId, _evening, 3, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(HoldResult.Refused(HoldRefusal.NotEnoughSeats));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new HoldSeatsCommand(_userId, _evening.Id, 3));

        // Assert
        result.Error.ShouldBe(ReservationErrors.NotEnoughSeats);
    }

    [Fact]
    [Trait("US", "2.3")]
    public async Task When_seats_are_free_Then_they_are_held_at_the_current_instant_in_UTC()
    {
        // Arrange: the repository answers only if it receives the current instant in UTC (16:00), not the local time (18:00)
        var seats = new[] { new Seat { Id = Guid.NewGuid(), Row = "C", Number = 5 }, new Seat { Id = Guid.NewGuid(), Row = "C", Number = 4 } };
        _reservations
            .Setup(r => r.TryHoldSeatsAsync(_userId, _evening, 2, NowUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(HoldResult.Held(Reservation.Hold(_userId, _evening, seats, NowUtc)));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new HoldSeatsCommand(_userId, _evening.Id, 2));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe("PendingPayment");
        result.Value.Seats.ShouldBe(new[] { "C4", "C5" });
        result.Value.TotalAmount.ShouldBe(19m);
        result.Value.ExpiresAt.ShouldBe(NowUtc.AddMinutes(10));
        result.Value.MovieId.ShouldBe(_evening.MovieId);
    }
}
