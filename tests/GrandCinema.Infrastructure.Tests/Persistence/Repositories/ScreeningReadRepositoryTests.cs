using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using GrandCinema.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Persistence.Repositories;

public class ScreeningReadRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _database;

    public ScreeningReadRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_screenings_exist_on_several_days_Then_only_those_of_the_chosen_day_are_returned()
    {
        // Arrange
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Blue Note", Synopsis = "A documentary about the jazz label", DurationMinutes = 136, Genre = "Documentary", ReleaseDate = new DateOnly(2023, 10, 1), PosterUrl = "https://example.com/poster.jpg" };
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        Screening At(DateTime startsAt) => new() { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9m };

        await using (var seed = _database.CreateDbContext())
        {
            seed.Screenings.AddRange(
                At(new DateTime(2026, 7, 18, 23, 30, 0, DateTimeKind.Unspecified)),
                At(new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Unspecified)),
                At(new DateTime(2026, 7, 19, 21, 0, 0, DateTimeKind.Unspecified)),
                At(new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Unspecified)));
            await seed.SaveChangesAsync();
        }

        await using var db = _database.CreateDbContext();
        var repository = new ScreeningReadRepository(db);

        // Act
        var result = await repository.GetScreeningsAsync(new DateOnly(2026, 7, 19));

        // Assert
        result.Select(s => s.StartsAt).ShouldBe(new[] { new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 19, 21, 0, 0, DateTimeKind.Unspecified) }, ignoreOrder: true);
        result.ShouldAllBe(s => s.Movie.Title == "Blue Note" && s.Room.Name == "Salle 1");
    }

    [Fact]
    [Trait("US", "2.2")]
    [Trait("Category", "Integration")]
    public async Task When_seats_are_sold_held_expired_or_cancelled_Then_only_sold_and_unexpired_held_seats_are_counted()
    {
        // Arrange: two screenings asked for, and a third one that is not
        var now = new DateTime(2026, 7, 18, 18, 0, 0, DateTimeKind.Utc);
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Whiplash", Synopsis = "A young jazz drummer", DurationMinutes = 107, Genre = "Drama", ReleaseDate = new DateOnly(2014, 1, 1), PosterUrl = "https://example.com/poster.jpg" };
        // Unique name: the rooms of the other tests of this class share the same database
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = 10 };
        var seats = Enumerable.Range(1, 8).Select(number => new Seat { Id = Guid.NewGuid(), Room = room, Row = "A", Number = number }).ToList();
        Screening At(int hour) => new() { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = new DateTime(2026, 7, 18, hour, 0, 0, DateTimeKind.Unspecified), Price = 9m };
        var evening = At(18);
        var night = At(21);
        var notAskedFor = At(23);
        Reservation Taking(Screening screening, ReservationStatus status, DateTime? expiresAt, params Seat[] taken)
        {
            var reservation = new Reservation { Id = Guid.NewGuid(), Screening = screening, Status = status, CreatedAt = now.AddMinutes(-5), ExpiresAt = expiresAt, TotalAmount = 9m * taken.Length };
            foreach (var seat in taken)
            {
                reservation.ReservedSeats.Add(new ReservedSeat { Seat = seat, ScreeningId = screening.Id });
            }
            return reservation;
        }

        await using (var seed = _database.CreateDbContext())
        {
            seed.Seats.AddRange(seats);
            seed.Reservations.AddRange(
                Taking(evening, ReservationStatus.Confirmed, null, seats[0], seats[1]),          // sold: taken
                Taking(evening, ReservationStatus.PendingPayment, now.AddMinutes(4), seats[2]),  // payment in progress: taken
                Taking(evening, ReservationStatus.PendingPayment, now, seats[3]),                // expires right now: free
                Taking(evening, ReservationStatus.PendingPayment, now.AddMinutes(-1), seats[4]), // expired, not cleaned yet: free
                Taking(evening, ReservationStatus.Expired, now.AddMinutes(-20), seats[5]),       // marked expired: free
                Taking(evening, ReservationStatus.Cancelled, null, seats[6]),                    // cancelled: free
                Taking(night, ReservationStatus.Confirmed, null, seats[0]),                      // another screening
                Taking(notAskedFor, ReservationStatus.Confirmed, null, seats[0]));               // not asked for
            await seed.SaveChangesAsync();
        }

        await using var db = _database.CreateDbContext();
        var repository = new ScreeningReadRepository(db);

        // Act
        var result = await repository.CountTakenSeatsAsync([evening.Id, night.Id], now);

        // Assert
        result.Count.ShouldBe(2);
        result[evening.Id].ShouldBe(3);
        result[night.Id].ShouldBe(1);
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_movie_has_past_and_upcoming_screenings_Then_only_its_upcoming_screenings_are_returned_with_their_room()
    {
        // Arrange: past, starting right now, a year ahead, and another movie
        var now = new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Unspecified);
        // Unique name: the rooms of the other tests of this class share the same database
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = 120 };
        Movie NewMovie(string title) => new() { Id = Guid.NewGuid(), Title = title, Synopsis = "Synopsis", DurationMinutes = 100, Genre = "Drama", ReleaseDate = new DateOnly(2025, 1, 1), PosterUrl = "https://example.com/p.jpg" };
        var bird = NewMovie("Bird");
        var other = NewMovie("Other");
        Screening At(Movie movie, DateTime startsAt) => new() { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9m };
        var ended = At(bird, now.AddHours(-2));
        var startsNow = At(bird, now);
        var nextYear = At(bird, now.AddYears(1));
        var otherMovie = At(other, now.AddDays(1));

        await using (var seed = _database.CreateDbContext())
        {
            seed.Screenings.AddRange(ended, startsNow, nextYear, otherMovie);
            await seed.SaveChangesAsync();
        }

        await using var db = _database.CreateDbContext();
        var repository = new ScreeningReadRepository(db);

        // Act
        var result = await repository.GetUpcomingScreeningsOfMovieAsync(bird.Id, now);

        // Assert
        result.Select(s => s.Id).ShouldBe(new[] { startsNow.Id, nextYear.Id }, ignoreOrder: true);
        result.ShouldAllBe(s => s.Room.Name == room.Name);
    }
}
