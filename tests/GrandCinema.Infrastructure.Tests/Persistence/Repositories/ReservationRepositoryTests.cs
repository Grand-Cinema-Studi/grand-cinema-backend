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
    private Task<Guid> ReservationAsync(Screening screening, ReservationStatus status, DateTime? expiresAt, params Seat[] seats) =>
        ReservationOfAsync(null, screening, status, expiresAt, seats);
    private async Task<Guid> ReservationOfAsync(Guid? userId, Screening screening, ReservationStatus status, DateTime? expiresAt, params Seat[] seats)
    {
        var reservation = new Reservation { Id = Guid.NewGuid(), UserId = userId, ScreeningId = screening.Id, Status = status, CreatedAt = Now.AddMinutes(-20), ExpiresAt = expiresAt, TotalAmount = 9.5m * seats.Length };
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

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_a_customer_already_has_paid_and_held_seats_Then_no_more_than_10_in_all_for_the_screening()
    {
        // Arrange: 16 seats. The customer has paid 4 seats, holds 3, and had held 5 that expired (they no longer count)
        var (screening, seats) = await ScreeningWithSeatsAsync(Enumerable.Range(1, 16).Select(number => ("A", number)).ToArray());
        var customer = (await CustomersAsync(1))[0];
        await ReservationOfAsync(customer, screening, ReservationStatus.Confirmed, null, seats["A1"], seats["A2"], seats["A3"], seats["A4"]);
        await ReservationOfAsync(customer, screening, ReservationStatus.PendingPayment, Now.AddMinutes(5), seats["A5"], seats["A6"], seats["A7"]);
        await ReservationOfAsync(customer, screening, ReservationStatus.PendingPayment, Now.AddMinutes(-1), seats["A8"], seats["A9"], seats["A10"], seats["A11"], seats["A12"]);
        await using var db = _database.CreateDbContext();
        var repository = new ReservationRepository(db);

        // Act: 4 more would make 11; 3 more make exactly 10
        var tooMany = await repository.TryHoldSeatsAsync(customer, screening, 4, Now);
        var exactly10 = await repository.TryHoldSeatsAsync(customer, screening, 3, Now);

        // Assert
        tooMany.Refusal.ShouldBe(HoldRefusal.SeatLimitReached);
        exactly10.Hold.ShouldNotBeNull();
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_two_tabs_of_the_same_customer_hold_10_seats_at_the_same_time_Then_only_one_succeeds()
    {
        // Arrange: 20 seats, one customer, two tabs released at the same instant
        var (screening, _) = await ScreeningWithSeatsAsync(Enumerable.Range(1, 20).Select(number => ("A", number)).ToArray());
        var customer = (await CustomersAsync(1))[0];
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act
        var tabs = Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            await using var db = _database.CreateDbContext();
            await start.Task;
            return await new ReservationRepository(db).TryHoldSeatsAsync(customer, screening, 10, Now);
        })).ToList();
        start.SetResult();
        var results = await Task.WhenAll(tabs);

        // Assert: counted under the lock, the second tab sees the 10 seats of the first one
        results.Count(result => result.Hold != null).ShouldBe(1);
        results.Count(result => result.Refusal == HoldRefusal.SeatLimitReached).ShouldBe(1);
    }

    [Fact]
    [Trait("US", "3.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_held_reservation_is_paid_Then_it_is_confirmed_and_keeps_its_seats()
    {
        // Arrange: a hold of 2 seats, still valid
        var (screening, seats) = await ScreeningWithSeatsAsync(("A", 1), ("A", 2));
        var hold = await ReservationAsync(screening, ReservationStatus.PendingPayment, Now.AddMinutes(5), seats["A1"], seats["A2"]);
        await using var db = _database.CreateDbContext();

        // Act
        var confirmation = await new ReservationRepository(db).ConfirmPaymentAsync(hold, "pi_paid", Now);

        // Assert: sold, with the trace of the payment, and its seats stay taken
        confirmation.ShouldBe(PaymentConfirmation.Confirmed);
        await using var readDb = _database.CreateDbContext();
        var paid = await readDb.Reservations.Include(r => r.ReservedSeats).SingleAsync(r => r.Id == hold);
        paid.Status.ShouldBe(ReservationStatus.Confirmed);
        paid.PaidAt.ShouldBe(Now);
        paid.PaymentReference.ShouldBe("pi_paid");
        paid.ReservedSeats.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("US", "3.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_seats_were_freed_before_the_payment_Then_it_is_rejected_and_nothing_changes()
    {
        // Arrange: the hold expired, and a later hold released its seat
        var (screening, _) = await ScreeningWithSeatsAsync(("A", 1));
        var freed = await ReservationAsync(screening, ReservationStatus.Expired, Now.AddMinutes(-15));
        await using var db = _database.CreateDbContext();

        // Act
        var confirmation = await new ReservationRepository(db).ConfirmPaymentAsync(freed, "pi_late", Now);

        // Assert: the handler refunds it
        confirmation.ShouldBe(PaymentConfirmation.Rejected);
        await using var readDb = _database.CreateDbContext();
        var kept = await readDb.Reservations.SingleAsync(r => r.Id == freed);
        kept.Status.ShouldBe(ReservationStatus.Expired);
        kept.PaymentReference.ShouldBeNull();
    }

    [Fact]
    [Trait("US", "3.1")]
    [Trait("Category", "Integration")]
    public async Task When_stripe_notifies_the_same_payment_twice_Then_it_is_confirmed_once_and_a_second_payment_is_rejected()
    {
        // Arrange
        var (screening, seats) = await ScreeningWithSeatsAsync(("A", 1));
        var hold = await ReservationAsync(screening, ReservationStatus.PendingPayment, Now.AddMinutes(5), seats["A1"]);
        await using var db = _database.CreateDbContext();
        var repository = new ReservationRepository(db);

        // Act: the notification sent again by Stripe, then a second payment from another tab
        var first = await repository.ConfirmPaymentAsync(hold, "pi_first", Now);
        var again = await repository.ConfirmPaymentAsync(hold, "pi_first", Now.AddSeconds(30));
        var secondPayment = await repository.ConfirmPaymentAsync(hold, "pi_second", Now.AddMinutes(1));

        // Assert
        first.ShouldBe(PaymentConfirmation.Confirmed);
        again.ShouldBe(PaymentConfirmation.AlreadyConfirmed);
        secondPayment.ShouldBe(PaymentConfirmation.Rejected);
    }

    [Fact]
    [Trait("US", "3.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_terms_of_sale_are_accepted_Then_their_version_and_instant_are_recorded()
    {
        // Arrange
        var (screening, seats) = await ScreeningWithSeatsAsync(("A", 1));
        var hold = await ReservationAsync(screening, ReservationStatus.PendingPayment, Now.AddMinutes(5), seats["A1"]);
        await using var db = _database.CreateDbContext();

        // Act
        await new ReservationRepository(db).RecordTermsOfSaleAsync(hold, "2026-11-02", Now);

        // Assert: the proof of criterion 4
        await using var readDb = _database.CreateDbContext();
        var accepted = await readDb.Reservations.SingleAsync(r => r.Id == hold);
        accepted.TermsOfSaleVersion.ShouldBe("2026-11-02");
        accepted.TermsOfSaleAcceptedAt.ShouldBe(Now);
    }

    [Fact]
    [Trait("US", "3.2")]
    [Trait("Category", "Integration")]
    public async Task When_a_payment_is_confirmed_Then_a_ticket_code_is_drawn_once_and_only_then()
    {
        // Arrange: a valid hold, and a hold whose seats were freed before its payment
        var (screening, seats) = await ScreeningWithSeatsAsync(("A", 1));
        var hold = await ReservationAsync(screening, ReservationStatus.PendingPayment, Now.AddMinutes(5), seats["A1"]);
        var freed = await ReservationAsync(screening, ReservationStatus.Expired, Now.AddMinutes(-15));
        await using var db = _database.CreateDbContext();
        var repository = new ReservationRepository(db);

        // Act: the payment, the same notification again, and a late payment
        await repository.ConfirmPaymentAsync(hold, "pi_paid", Now);
        string? CodeOf(Guid id)
        {
            using var readDb = _database.CreateDbContext();
            return readDb.Reservations.Single(r => r.Id == id).TicketCode;
        }
        var drawn = CodeOf(hold);
        await repository.ConfirmPaymentAsync(hold, "pi_paid", Now.AddSeconds(30));
        await repository.ConfirmPaymentAsync(freed, "pi_late", Now);

        // Assert: criterion 1 and criterion 3
        drawn.ShouldNotBeNull();
        drawn.ShouldMatch("^[0-9a-f]{32}$");
        CodeOf(hold).ShouldBe(drawn);
        CodeOf(freed).ShouldBeNull();
    }

    [Fact]
    [Trait("US", "4.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_ticket_is_scanned_Then_it_is_used_once_and_each_refusal_says_why()
    {
        // Arrange: a paid ticket of the 20:30 screening, and the 22:45 screening of the same room
        var (screening, seats) = await ScreeningWithSeatsAsync(("A", 1));
        var (otherScreening, _) = await ScreeningWithSeatsAsync(("A", 1));
        var reservationId = await ReservationAsync(screening, ReservationStatus.PendingPayment, Now.AddMinutes(5), seats["A1"]);
        await using (var pay = _database.CreateDbContext())
        {
            await new ReservationRepository(pay).ConfirmPaymentAsync(reservationId, "pi_paid", Now);
        }
        await using var readDb = _database.CreateDbContext();
        var code = (await readDb.Reservations.SingleAsync(r => r.Id == reservationId)).TicketCode!;
        await using var db = _database.CreateDbContext();
        var repository = new ReservationRepository(db);

        // Act: at the wrong door, at the right one, a second time, and a code that does not exist
        var atOtherDoor = await repository.UseTicketAsync(code, otherScreening.Id, Now.AddHours(4));
        var atItsDoor = await repository.UseTicketAsync(code, screening.Id, Now.AddHours(4));
        var again = await repository.UseTicketAsync(code, screening.Id, Now.AddHours(4).AddMinutes(1));
        var unknown = await repository.UseTicketAsync(Reservation.NewTicketCode(), screening.Id, Now.AddHours(4));

        // Assert: criteria 1, 2 and 3; the time of the passage is recorded once
        atOtherDoor.ShouldBe(TicketUse.OtherScreening);
        atItsDoor.ShouldBe(TicketUse.Accepted);
        again.ShouldBe(TicketUse.AlreadyUsed);
        unknown.ShouldBe(TicketUse.NotFound);
        await using var checkDb = _database.CreateDbContext();
        (await checkDb.Reservations.SingleAsync(r => r.Id == reservationId)).TicketUsedAt.ShouldBe(Now.AddHours(4));
    }

    [Fact]
    [Trait("US", "4.1")]
    [Trait("Category", "Integration")]
    public async Task When_two_inspectors_scan_the_same_ticket_at_once_Then_only_one_lets_it_in()
    {
        // Arrange: a paid ticket
        var (screening, seats) = await ScreeningWithSeatsAsync(("A", 1));
        var reservationId = await ReservationAsync(screening, ReservationStatus.PendingPayment, Now.AddMinutes(5), seats["A1"]);
        await using (var pay = _database.CreateDbContext())
        {
            await new ReservationRepository(pay).ConfirmPaymentAsync(reservationId, "pi_paid", Now);
        }
        await using var readDb = _database.CreateDbContext();
        var code = (await readDb.Reservations.SingleAsync(r => r.Id == reservationId)).TicketCode!;

        // Act: two scans at the same instant, each with its own connection, as two phones at two doors
        async Task<TicketUse> ScanAsync()
        {
            await using var db = _database.CreateDbContext();
            return await new ReservationRepository(db).UseTicketAsync(code, screening.Id, Now.AddHours(4));
        }
        var uses = await Task.WhenAll(ScanAsync(), ScanAsync());

        // Assert: the ticket lets one person in, never two
        uses.Count(use => use == TicketUse.Accepted).ShouldBe(1);
        uses.Count(use => use == TicketUse.AlreadyUsed).ShouldBe(1);
    }
}
