using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Reservations;
using GrandCinema.Application.Features.Reservations.Commands;
using GrandCinema.Application.Features.Reservations.Queries;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Reservations.Commands;

public class CheckTicketHandlerTests
{
    [Fact]
    [Trait("US", "4.1")]
    public async Task When_the_text_is_not_a_ticket_code_Then_the_database_is_not_queried()
    {
        // Arrange: a QR code of something else
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        var queries = new Mock<ITicketQueries>(MockBehavior.Strict);
        var handler = new CheckTicketHandler(repository.Object, queries.Object, new FakeTimeProvider());

        // Act
        var result = await handler.Handle(new CheckTicketCommand(Guid.NewGuid(), "WIFI:S:cinema;T:WPA;P:secret;;"));

        // Assert: criterion 3, refused as unknown; Strict mocks fail on any call
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ReservationErrors.TicketNotFound);
    }
}
