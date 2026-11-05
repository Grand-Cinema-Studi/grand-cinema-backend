using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Reservations.Queries;

public class GetReservationTrackingHandlerTests
{
    [Fact]
    [Trait("US", "5.3")]
    public async Task When_no_date_is_given_Then_today_of_the_cinema_is_followed_at_the_current_instant()
    {
        // Arrange: 22:30 UTC on 18 July is 00:30 on 19 July in Paris
        var now = new DateTimeOffset(2026, 7, 18, 22, 30, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(now);
        time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));
        var screenings = new List<ScreeningTrackingDto>
        {
            new() { ScreeningId = Guid.NewGuid(), StartsAt = new DateTime(2026, 7, 19, 14, 0, 0, DateTimeKind.Unspecified), MovieTitle = "Bird", RoomName = "Salle 1", Capacity = 170, SoldOnline = 40, SoldAtCounter = 10, HeldSeats = 2 }
        };
        var queries = new Mock<IReservationTrackingQueries>();
        queries
            .Setup(q => q.GetScreeningsOfDayAsync(new DateOnly(2026, 7, 19), now.UtcDateTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(screenings);
        var handler = new GetReservationTrackingHandler(queries.Object, time);

        // Act
        var result = await handler.Handle(new GetReservationTrackingQuery(null));

        // Assert: the day of the cinema, the holds compared with the instant in UTC, and what is left on sale
        result.Date.ShouldBe(new DateOnly(2026, 7, 19));
        result.Screenings.ShouldBe(screenings);
        result.Screenings[0].RemainingSeats.ShouldBe(118);
    }
}
