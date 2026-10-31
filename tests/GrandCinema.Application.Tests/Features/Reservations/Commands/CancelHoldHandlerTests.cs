using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Reservations;
using GrandCinema.Application.Features.Reservations.Commands;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Reservations.Commands;

public class CancelHoldHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 7, 18, 16, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Reservation _hold;

    public CancelHoldHandlerTests()
    {
        var screening = new Screening { Id = Guid.NewGuid(), MovieId = Guid.NewGuid(), StartsAt = new DateTime(2026, 7, 18, 20, 30, 0, DateTimeKind.Unspecified), Price = 9.5m };
        _hold = Reservation.Hold(_owner, screening, new[] { new Seat { Id = Guid.NewGuid(), Row = "A", Number = 1 } }, NowUtc);
        _reservations
            .Setup(r => r.GetByIdAsync(_hold.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_hold);
    }

    private CancelHoldHandler CreateHandler() => new(_reservations.Object);

    private void NothingWasCancelled() =>
        _reservations.Verify(r => r.CancelHoldAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    [Trait("US", "2.3")]
    public async Task When_the_owner_cancels_a_hold_Then_its_seats_are_freed()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new CancelHoldCommand(_owner, _hold.Id));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _reservations.Verify(r => r.CancelHoldAsync(_hold.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("US", "2.3")]
    public async Task When_another_customer_cancels_the_hold_Then_reservation_not_found_and_nothing_is_cancelled()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new CancelHoldCommand(Guid.NewGuid(), _hold.Id));

        // Assert
        result.Error.ShouldBe(ReservationErrors.ReservationNotFound);
        NothingWasCancelled();
    }

    [Fact]
    [Trait("US", "2.3")]
    public async Task When_the_reservation_is_paid_Then_it_cannot_be_cancelled_online()
    {
        // Arrange
        _hold.Status = ReservationStatus.Confirmed;
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new CancelHoldCommand(_owner, _hold.Id));

        // Assert
        result.Error.ShouldBe(ReservationErrors.ReservationConfirmed);
        NothingWasCancelled();
    }
}
