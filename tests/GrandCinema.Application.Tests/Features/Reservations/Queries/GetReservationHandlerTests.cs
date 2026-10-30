using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Reservations;
using GrandCinema.Application.Features.Reservations.Queries;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Reservations.Queries;

public class GetReservationHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 7, 18, 16, 0, 0, DateTimeKind.Utc);
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(NowUtc));
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Reservation _hold;

    public GetReservationHandlerTests()
    {
        // Held 12 minutes ago: the 10 minutes have run out, but no other hold has marked it "Expired" yet
        var screening = new Screening { Id = Guid.NewGuid(), StartsAt = new DateTime(2026, 7, 18, 20, 30, 0, DateTimeKind.Unspecified), Price = 9.5m };
        _hold = Reservation.Hold(_owner, screening, new[] { new Seat { Id = Guid.NewGuid(), Row = "A", Number = 1 } }, NowUtc.AddMinutes(-12));
        _reservations
            .Setup(r => r.GetByIdAsync(_hold.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_hold);
    }

    private GetReservationHandler CreateHandler() => new(_reservations.Object, _time);

    [Fact]
    [Trait("US", "2.3")]
    public async Task When_the_owner_asks_for_an_expired_hold_Then_it_is_returned_as_expired()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetReservationQuery(_owner, _hold.Id));

        // Assert: the front tells the customer that the seats were freed (criterion 5)
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe("Expired");
        result.Value.Seats.ShouldBe(new[] { "A1" });
    }

    [Fact]
    [Trait("US", "2.3")]
    public async Task When_another_customer_asks_for_the_reservation_Then_reservation_not_found()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetReservationQuery(Guid.NewGuid(), _hold.Id));

        // Assert
        result.Error.ShouldBe(ReservationErrors.ReservationNotFound);
    }
}
