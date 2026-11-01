using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using GrandCinema.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Persistence.Repositories;

public class ReservationRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private static readonly DateTime Now = new(2026, 7, 18, 16, 0, 0, DateTimeKind.Utc);
    private readonly PostgreSqlFixture _database;

    public ReservationRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    // A screening in its own room, whose seats are inserted in the order given (not the order of the room)
    private async Task<(Screening Screening, Dictionary<string, Seat> Seats)> ScreeningWithSeatsAsync(params (string Row, int Number)[] places)
    {
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = places.Length };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Bird", Synopsis = "Charlie Parker", DurationMinutes = 161, Genre = "Drama", ReleaseDate = new DateOnly(1988, 9, 30), PosterUrl = "https://example.com/poster.jpg" };
        var screening = new Screening { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = new DateTime(2026, 7, 18, 20, 30, 0, DateTimeKind.Unspecified), Price = 9.5m };
        var seats = places.Select(place => new Seat { Id = Guid.NewGuid(), Room = room, Row = place.Row, Number = place.Number }).ToList();
        await using var seed = _database.CreateDbContext();
        seed.Seats.AddRange(seats);
        seed.Screenings.Add(screening);
        await seed.SaveChangesAsync();
        return (screening, seats.ToDictionary(seat => $"{seat.Row}{seat.Number}"));
    }

    // Identifiers only: the screening and the seats already exist and must not be inserted again
    private async Task<Guid> ReservationAsync(Screening screening, ReservationStatus status, DateTime? expiresAt, params Seat[] seats)
    {
        var reservation = new Reservation { Id = Guid.NewGuid(), ScreeningId = screening.Id, Status = status, CreatedAt = Now.AddMinutes(-20), ExpiresAt = expiresAt, TotalAmount = 9.5m * seats.Length };
        foreach (var seat in seats)
        {
            reservation.ReservedSeats.Add(new ReservedSeat { SeatId = seat.Id, ScreeningId = screening.Id });
        }

        await using var seed = _database.CreateDbContext();
        seed.Reservations.Add(reservation);
        await seed.SaveChangesAsync();
        return reservation.Id;
    }

    // Reservations.UserId is a foreign key: a hold belongs to an existing account
    private async Task<List<Guid>> CustomersAsync(int count)
    {
        var users = Enumerable.Range(0, count).Select(_ => new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@example.com",
            PasswordHash = "hashed-password",
            FirstName = "Léa",
            LastName = "Martin",
            CreatedAt = DateTime.UtcNow,
            Role = UserRole.Customer
        }).ToList();
        await using var seed = _database.CreateDbContext();
        seed.Users.AddRange(users);
        await seed.SaveChangesAsync();
        return users.Select(user => user.Id).ToList();
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_seats_are_held_Then_the_first_free_seats_are_given_row_after_row_and_held_for_10_minutes()
    {
        // Arrange: seats inserted out of order; A1 already sold at the counter
        var (screening, seats) = await ScreeningWithSeatsAsync(("AA", 1), ("B", 1), ("A", 2), ("A", 1));
        await ReservationAsync(screening, ReservationStatus.Confirmed, null, seats["A1"]);
        var customer = (await CustomersAsync(1))[0];
        await using var db = _database.CreateDbContext();

        // Act
        var hold = (await new ReservationRepository(db).TryHoldSeatsAsync(customer, screening, 2, Now)).Hold;

        // Assert: A2 then B1; AA1 comes after Z, and the sold A1 is skipped
        hold.ShouldNotBeNull();
        await using var readDb = _database.CreateDbContext();
        var saved = await new ReservationRepository(readDb).GetByIdAsync(hold.Id);
        saved.ShouldNotBeNull();
        saved.Screening.ShouldNotBeNull().MovieId.ShouldBe(screening.MovieId);
        saved.UserId.ShouldBe(customer);
        saved.Status.ShouldBe(ReservationStatus.PendingPayment);
        saved.ExpiresAt.ShouldBe(Now.AddMinutes(10));
        saved.ReservedSeats.Select(reserved => reserved.Seat.Id).ShouldBe(new[] { seats["A2"].Id, seats["B1"].Id }, ignoreOrder: true);
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_fewer_seats_are_free_than_asked_Then_nothing_is_held()
    {
        // Arrange: 2 seats, 1 sold
        var (screening, seats) = await ScreeningWithSeatsAsync(("A", 1), ("A", 2));
        await ReservationAsync(screening, ReservationStatus.Confirmed, null, seats["A1"]);
        var customer = (await CustomersAsync(1))[0];
        await using var db = _database.CreateDbContext();

        // Act
        var result = await new ReservationRepository(db).TryHoldSeatsAsync(customer, screening, 2, Now);

        // Assert: no partial hold
        result.Refusal.ShouldBe(HoldRefusal.NotEnoughSeats);
        await using var readDb = _database.CreateDbContext();
        (await readDb.Reservations.CountAsync(r => r.ScreeningId == screening.Id)).ShouldBe(1);
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_a_hold_has_expired_Then_its_seat_is_given_again_and_it_is_marked_expired()
    {
        // Arrange: the only seat was held by a payment abandoned 11 minutes ago
        var (screening, seats) = await ScreeningWithSeatsAsync(("A", 1));
        var abandoned = await ReservationAsync(screening, ReservationStatus.PendingPayment, Now.AddMinutes(-1), seats["A1"]);
        var customer = (await CustomersAsync(1))[0];
        await using var db = _database.CreateDbContext();

        // Act
        var hold = (await new ReservationRepository(db).TryHoldSeatsAsync(customer, screening, 1, Now)).Hold;

        // Assert: the seat goes to the new customer; the abandoned hold is kept, expired and without seat
        hold.ShouldNotBeNull();
        await using var readDb = _database.CreateDbContext();
        var old = await readDb.Reservations.Include(r => r.ReservedSeats).SingleAsync(r => r.Id == abandoned);
        old.Status.ShouldBe(ReservationStatus.Expired);
        old.ReservedSeats.ShouldBeEmpty();
        (await readDb.ReservedSeats.SingleAsync(reserved => reserved.ScreeningId == screening.Id)).ReservationId.ShouldBe(hold.Id);
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_ten_customers_hold_a_seat_at_the_same_time_Then_each_one_gets_a_different_seat()
    {
        // Arrange: 10 seats, 10 customers, and a starting signal
        var (screening, _) = await ScreeningWithSeatsAsync(Enumerable.Range(1, 10).Select(number => ("A", number)).ToArray());
        var customers = await CustomersAsync(10);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act: 10 holds on 10 connections, released at the same instant
        var holding = customers.Select(customer => Task.Run(async () =>
        {
            await using var db = _database.CreateDbContext();
            await start.Task;
            return (await new ReservationRepository(db).TryHoldSeatsAsync(customer, screening, 1, Now)).Hold;
        })).ToList();
        start.SetResult();
        var holds = await Task.WhenAll(holding);

        // Assert: without the lock, several holds pick the same first free seat, and the unique index refuses all of them but one
        holds.ShouldAllBe(hold => hold != null);
        holds.SelectMany(hold => hold!.ReservedSeats).Select(reserved => reserved.SeatId).Distinct().Count().ShouldBe(10);
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_a_hold_is_cancelled_Then_it_is_marked_cancelled_and_its_seat_can_be_held_again_at_once()
    {
        // Arrange: the only seat is held by a first customer
        var (screening, _) = await ScreeningWithSeatsAsync(("A", 1));
        var customers = await CustomersAsync(2);
        Reservation? first;
        await using (var db = _database.CreateDbContext())
        {
            first = (await new ReservationRepository(db).TryHoldSeatsAsync(customers[0], screening, 1, Now)).Hold;
        }

        first.ShouldNotBeNull();

        // Act
        await using (var db = _database.CreateDbContext())
        {
            (await new ReservationRepository(db).CancelHoldAsync(first.Id)).ShouldBeTrue();
        }

        // Assert: the hold is kept, cancelled and without seat; the seat is on sale again at once
        await using var readDb = _database.CreateDbContext();
        var cancelled = await readDb.Reservations.Include(r => r.ReservedSeats).SingleAsync(r => r.Id == first.Id);
        cancelled.Status.ShouldBe(ReservationStatus.Cancelled);
        cancelled.ReservedSeats.ShouldBeEmpty();
        (await new ReservationRepository(readDb).TryHoldSeatsAsync(customers[1], screening, 1, Now)).Hold.ShouldNotBeNull();
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_the_reservation_is_paid_Then_it_is_not_cancelled()
    {
        // Arrange
        var (screening, seats) = await ScreeningWithSeatsAsync(("A", 1));
        var paid = await ReservationAsync(screening, ReservationStatus.Confirmed, null, seats["A1"]);
        await using var db = _database.CreateDbContext();

        // Act
        var cancelled = await new ReservationRepository(db).CancelHoldAsync(paid);

        // Assert
        cancelled.ShouldBeFalse();
        await using var readDb = _database.CreateDbContext();
        var kept = await readDb.Reservations.Include(r => r.ReservedSeats).SingleAsync(r => r.Id == paid);
        kept.Status.ShouldBe(ReservationStatus.Confirmed);
        kept.ReservedSeats.Count.ShouldBe(1);
    }
}
