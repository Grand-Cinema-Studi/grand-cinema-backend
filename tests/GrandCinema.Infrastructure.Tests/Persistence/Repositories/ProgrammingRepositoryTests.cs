using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using GrandCinema.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Persistence.Repositories;

public class ProgrammingRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private static readonly DateTime Now = new(2026, 7, 18, 16, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day = new(2026, 7, 18, 0, 0, 0, DateTimeKind.Unspecified);
    private readonly PostgreSqlFixture _database;

    public ProgrammingRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    // A screening of Bird (161 minutes) in its own room of seatCount seats (row A)
    private async Task<(Screening Screening, List<Seat> Seats)> ScreeningAsync(DateTime startsAt, int seatCount = 2)
    {
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = seatCount };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Bird", Synopsis = "Charlie Parker", DurationMinutes = 161, Genre = "Drama", ReleaseDate = new DateOnly(1988, 9, 30), PosterUrl = "https://example.com/poster.jpg" };
        var screening = new Screening { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9.5m };
        var seats = Enumerable.Range(1, seatCount).Select(number => new Seat { Id = Guid.NewGuid(), Room = room, Row = "A", Number = number }).ToList();
        await using var seed = _database.CreateDbContext();
        seed.Seats.AddRange(seats);
        seed.Screenings.Add(screening);
        await seed.SaveChangesAsync();
        return (screening, seats);
    }

    // A counter sale or a hold, without any account; identifiers only
    private async Task<Guid> ReservationAsync(Screening screening, ReservationStatus status, DateTime? expiresAt, Seat seat)
    {
        var reservation = new Reservation { Id = Guid.NewGuid(), ScreeningId = screening.Id, Status = status, CreatedAt = Now.AddMinutes(-5), ExpiresAt = expiresAt, TotalAmount = 9.5m };
        reservation.ReservedSeats.Add(new ReservedSeat { SeatId = seat.Id, ScreeningId = screening.Id });
        await using var seed = _database.CreateDbContext();
        seed.Reservations.Add(reservation);
        await seed.SaveChangesAsync();
        return reservation.Id;
    }

    private async Task CancelAsync(Screening screening)
    {
        await using var db = _database.CreateDbContext();
        await new ProgrammingRepository(db).CancelScreeningAsync(screening.Id, Now);
    }

    [Fact]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    public async Task When_a_screening_is_added_Then_it_is_read_with_its_movie_among_the_screenings_of_its_room()
    {
        // Arrange: a room and a movie that exist, without any screening yet
        var (existing, _) = await ScreeningAsync(Day.AddHours(14));
        var added = new Screening { Id = Guid.NewGuid(), MovieId = existing.Movie.Id, RoomId = existing.Room.Id, StartsAt = Day.AddHours(20), Price = 11m };

        // Act
        await using (var db = _database.CreateDbContext())
        {
            await new ProgrammingRepository(db).AddScreeningAsync(added);
        }

        // Assert: both screenings of the room that day, each with its movie
        await using var read = _database.CreateDbContext();
        var screenings = await new ProgrammingRepository(read).GetScreeningsOfRoomAsync(existing.Room.Id, Day, Day.AddDays(1));
        screenings.Select(s => s.Id).ShouldBe(new[] { existing.Id, added.Id }, ignoreOrder: true);
        screenings.ShouldAllBe(s => s.Movie.DurationMinutes == 161);
    }

    [Fact]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    public async Task When_a_screening_is_moved_Then_its_new_time_and_price_are_saved()
    {
        // Arrange
        var (screening, _) = await ScreeningAsync(Day.AddHours(18));

        // Act
        await using (var db = _database.CreateDbContext())
        {
            await new ProgrammingRepository(db).UpdateScreeningAsync(screening.Id, Day.AddHours(21), 11m);
        }

        // Assert
        await using var read = _database.CreateDbContext();
        var moved = await new ProgrammingRepository(read).GetScreeningAsync(screening.Id);
        moved.ShouldNotBeNull();
        moved.StartsAt.ShouldBe(Day.AddHours(21));
        moved.Price.ShouldBe(11m);
        moved.Movie.Title.ShouldBe("Bird");
    }

    [Fact]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    public async Task When_a_screening_is_cancelled_Then_it_leaves_the_schedule_and_can_no_longer_be_found()
    {
        // Arrange
        var (screening, _) = await ScreeningAsync(Day.AddHours(20));

        // Act
        await CancelAsync(screening);

        // Assert: neither the public schedule (US 1.1), nor a hold (US 2.3), nor the programming find it
        await using var read = _database.CreateDbContext();
        (await new ScreeningReadRepository(read).GetScreeningsAsync(DateOnly.FromDateTime(Day))).ShouldNotContain(s => s.Id == screening.Id);
        (await new ScreeningReadRepository(read).GetByIdAsync(screening.Id)).ShouldBeNull();
        (await new ProgrammingRepository(read).GetScreeningAsync(screening.Id)).ShouldBeNull();
        // It still exists, with the instant of its cancellation
        var cancelledAt = await read.Screenings.IgnoreQueryFilters().Where(s => s.Id == screening.Id).Select(s => s.CancelledAt).SingleAsync();
        cancelledAt.ShouldBe(Now);
    }

    [Fact]
    [Trait("US", "5.2")]
    [Trait("Category", "Integration")]
    public async Task When_a_screening_is_cancelled_Then_its_holds_are_cancelled_and_its_sales_are_kept_and_readable()
    {
        // Arrange: A1 held for 5 more minutes, A2 sold
        var (screening, seats) = await ScreeningAsync(Day.AddHours(20));
        var hold = await ReservationAsync(screening, ReservationStatus.PendingPayment, Now.AddMinutes(5), seats[0]);
        var sale = await ReservationAsync(screening, ReservationStatus.Confirmed, null, seats[1]);

        // Act
        await CancelAsync(screening);

        // Assert: the hold is cancelled and its seat freed; the sale keeps its seat, for the history and the refund
        await using var read = _database.CreateDbContext();
        var statuses = await read.Reservations.Where(r => r.ScreeningId == screening.Id).ToDictionaryAsync(r => r.Id, r => r.Status);
        statuses[hold].ShouldBe(ReservationStatus.Cancelled);
        statuses[sale].ShouldBe(ReservationStatus.Confirmed);
        (await read.ReservedSeats.Where(reserved => reserved.ScreeningId == screening.Id).Select(reserved => reserved.ReservationId).ToListAsync())
            .ShouldBe(new[] { sale });
        // The customer still reads what they paid for, although the screening has left the schedule
        var paid = await new ReservationRepository(read).GetByIdAsync(sale);
        paid.ShouldNotBeNull().Screening.Id.ShouldBe(screening.Id);
    }

    [Fact]
    [Trait("US", "5.6")]
    [Trait("Category", "Integration")]
    public async Task When_a_movie_is_added_Then_it_exists_whatever_the_case_of_its_title_but_not_on_another_release_date()
    {
        // Arrange: a title of its own, the database is shared by the tests of the class
        var title = $"Bird {Guid.NewGuid():N}";
        var movie = new Movie { Id = Guid.NewGuid(), Title = title, Synopsis = "Charlie Parker", DurationMinutes = 161, Genre = "Drama", ReleaseDate = new DateOnly(1988, 9, 30), PosterUrl = "/posters/bird.png" };

        // Act
        await using (var db = _database.CreateDbContext())
        {
            await new ProgrammingRepository(db).AddMovieAsync(movie);
        }

        // Assert: read back by a new context, as the next request does; upper() is run by PostgreSQL
        await using var read = _database.CreateDbContext();
        var repository = new ProgrammingRepository(read);
        (await repository.MovieExistsAsync(title.ToUpperInvariant(), movie.ReleaseDate)).ShouldBeTrue();
        (await repository.MovieExistsAsync(title, movie.ReleaseDate.AddYears(1))).ShouldBeFalse();
        (await read.Movies.SingleAsync(m => m.Id == movie.Id)).PosterUrl.ShouldBe("/posters/bird.png");
    }
}
