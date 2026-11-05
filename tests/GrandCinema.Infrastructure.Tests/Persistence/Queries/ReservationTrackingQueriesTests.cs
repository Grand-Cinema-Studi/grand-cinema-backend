using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using GrandCinema.Infrastructure.Persistence.Queries;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Persistence.Queries;

public class ReservationTrackingQueriesTests : IClassFixture<PostgreSqlFixture>
{
    private static readonly DateTime Now = new(2026, 7, 18, 16, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day = new(2026, 7, 18, 0, 0, 0, DateTimeKind.Unspecified);
    private readonly PostgreSqlFixture _database;

    public ReservationTrackingQueriesTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    [Trait("US", "5.3")]
    [Trait("Category", "Integration")]
    public async Task When_a_day_is_followed_Then_seats_are_counted_by_kind_for_each_screening_and_cancelled_screenings_are_left_out()
    {
        // Arrange: a room of 6 seats; Bird at 20:30 with 2 seats sold online, 1 at the counter, 1 held, 1 held but expired;
        // Metropolis at 14:00 without any reservation; a cancelled screening, and one on the next day
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 6 };
        Movie NewMovie(string title) => new() { Id = Guid.NewGuid(), Title = title, Synopsis = "Synopsis", DurationMinutes = 100, Genre = "Drama", ReleaseDate = new DateOnly(2025, 1, 1), PosterUrl = "https://example.com/p.jpg" };
        var seats = Enumerable.Range(1, 6).Select(number => new Seat { Id = Guid.NewGuid(), Room = room, Row = "A", Number = number }).ToList();
        var bird = new Screening { Id = Guid.NewGuid(), Movie = NewMovie("Bird"), Room = room, StartsAt = Day.AddHours(20.5), Price = 9.5m };
        var metropolis = new Screening { Id = Guid.NewGuid(), Movie = NewMovie("Metropolis"), Room = room, StartsAt = Day.AddHours(14), Price = 9.5m };
        var cancelled = new Screening { Id = Guid.NewGuid(), Movie = NewMovie("Cancelled"), Room = room, StartsAt = Day.AddHours(10), Price = 9.5m, CancelledAt = Now.AddDays(-1) };
        var tomorrow = new Screening { Id = Guid.NewGuid(), Movie = NewMovie("Tomorrow"), Room = room, StartsAt = Day.AddDays(1).AddHours(20), Price = 9.5m };
        Reservation Of(ReservationStatus status, string? paymentReference, DateTime? expiresAt, params Seat[] held)
        {
            var reservation = new Reservation { Id = Guid.NewGuid(), ScreeningId = bird.Id, Status = status, CreatedAt = Now.AddMinutes(-20), ExpiresAt = expiresAt, TotalAmount = 9.5m * held.Length, PaymentReference = paymentReference };
            foreach (var seat in held)
            {
                reservation.ReservedSeats.Add(new ReservedSeat { SeatId = seat.Id, ScreeningId = bird.Id });
            }
            return reservation;
        }

        await using (var seed = _database.CreateDbContext())
        {
            seed.Seats.AddRange(seats);
            seed.Screenings.AddRange(bird, metropolis, cancelled, tomorrow);
            await seed.SaveChangesAsync();
            seed.Reservations.AddRange(
                Of(ReservationStatus.Confirmed, "pi_online", null, seats[0], seats[1]),
                Of(ReservationStatus.Confirmed, null, null, seats[2]),
                Of(ReservationStatus.PendingPayment, null, Now.AddMinutes(5), seats[3]),
                Of(ReservationStatus.PendingPayment, null, Now.AddMinutes(-1), seats[4]));
            await seed.SaveChangesAsync();
        }

        // Act
        await using var db = _database.CreateDbContext();
        var screenings = await new ReservationTrackingQueries(db).GetScreeningsOfDayAsync(DateOnly.FromDateTime(Day), Now);

        // Assert: by time; the expired hold frees its seat; the cancelled screening and the next day are not there
        screenings.Select(s => s.MovieTitle).ShouldBe(new[] { "Metropolis", "Bird" });
        var metropolisLine = screenings[0];
        (metropolisLine.SoldOnline, metropolisLine.SoldAtCounter, metropolisLine.HeldSeats, metropolisLine.RemainingSeats).ShouldBe((0, 0, 0, 6));
        var birdLine = screenings[1];
        birdLine.ScreeningId.ShouldBe(bird.Id);
        birdLine.RoomName.ShouldBe("Salle 1");
        birdLine.Capacity.ShouldBe(6);
        (birdLine.SoldOnline, birdLine.SoldAtCounter, birdLine.HeldSeats, birdLine.RemainingSeats).ShouldBe((2, 1, 1, 2));
    }
}
