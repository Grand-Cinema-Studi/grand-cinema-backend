using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using GrandCinema.Infrastructure.Persistence.Queries;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Persistence.Queries;

public class TicketQueriesTests : IClassFixture<PostgreSqlFixture>
{
    private static readonly DateTime Now = new(2026, 7, 18, 16, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day = new(2026, 7, 18, 0, 0, 0, DateTimeKind.Unspecified);
    private readonly PostgreSqlFixture _database;

    public TicketQueriesTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    [Trait("US", "3.2")]
    [Trait("Category", "Integration")]
    public async Task When_the_tickets_of_a_customer_are_read_Then_only_their_paid_reservations_are_returned_cancelled_screenings_included()
    {
        // Arrange: Léa paid Bird (2 seats) and Metropolis, whose screening was then cancelled; she holds Nosferatu
        // without paying; Paul paid Bird too
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 6 };
        Movie NewMovie(string title) => new() { Id = Guid.NewGuid(), Title = title, Synopsis = "Synopsis", DurationMinutes = 100, Genre = "Drama", ReleaseDate = new DateOnly(2025, 1, 1), PosterUrl = "https://example.com/p.jpg" };
        var seats = Enumerable.Range(1, 6).Select(number => new Seat { Id = Guid.NewGuid(), Room = room, Row = "A", Number = number }).ToList();
        var bird = new Screening { Id = Guid.NewGuid(), Movie = NewMovie("Bird"), Room = room, StartsAt = Day.AddHours(20.5), Price = 9.5m };
        var metropolis = new Screening { Id = Guid.NewGuid(), Movie = NewMovie("Metropolis"), Room = room, StartsAt = Day.AddDays(1).AddHours(14), Price = 9.5m, CancelledAt = Now };
        var nosferatu = new Screening { Id = Guid.NewGuid(), Movie = NewMovie("Nosferatu"), Room = room, StartsAt = Day.AddDays(2).AddHours(22), Price = 9.5m };
        User NewUser(string firstName) => new() { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid()}@example.com", PasswordHash = "hashed-password", FirstName = firstName, LastName = "Martin", CreatedAt = Now, Role = UserRole.Customer };
        var lea = NewUser("Léa");
        var paul = NewUser("Paul");
        Reservation Of(User user, Screening screening, ReservationStatus status, string? ticketCode, params Seat[] held)
        {
            var reservation = new Reservation { Id = Guid.NewGuid(), UserId = user.Id, ScreeningId = screening.Id, Status = status, CreatedAt = Now.AddHours(-1), ExpiresAt = Now.AddMinutes(5), TotalAmount = 9.5m * held.Length, TicketCode = ticketCode };
            foreach (var seat in held)
            {
                reservation.ReservedSeats.Add(new ReservedSeat { SeatId = seat.Id, ScreeningId = screening.Id });
            }
            return reservation;
        }

        var leaBird = Of(lea, bird, ReservationStatus.Confirmed, Reservation.NewTicketCode(), seats[0], seats[1]);
        var leaMetropolis = Of(lea, metropolis, ReservationStatus.Confirmed, Reservation.NewTicketCode(), seats[0]);
        await using (var seed = _database.CreateDbContext())
        {
            seed.Users.AddRange(lea, paul);
            seed.Seats.AddRange(seats);
            seed.Screenings.AddRange(bird, metropolis, nosferatu);
            await seed.SaveChangesAsync();
            seed.Reservations.AddRange(
                leaBird,
                leaMetropolis,
                Of(lea, nosferatu, ReservationStatus.PendingPayment, null, seats[0]),
                Of(paul, bird, ReservationStatus.Confirmed, Reservation.NewTicketCode(), seats[2]));
            await seed.SaveChangesAsync();
        }

        // Act
        await using var db = _database.CreateDbContext();
        var queries = new TicketQueries(db);
        var tickets = await queries.GetTicketsOfUserAsync(lea.Id);
        var birdTicket = await queries.GetTicketAsync(leaBird.Id, lea.Id);

        // Assert: the latest screening first; the cancelled one is there, and says so; the unpaid one and Paul's are not
        tickets.Select(t => (t.MovieTitle, t.SeatCount, t.ScreeningCancelled)).ShouldBe(new[] { ("Metropolis", 1, true), ("Bird", 2, false) });
        birdTicket.ShouldNotBeNull();
        birdTicket.TicketCode.ShouldBe(leaBird.TicketCode);
        birdTicket.RoomName.ShouldBe("Salle 1");
        birdTicket.StartsAt.ShouldBe(bird.StartsAt);
        birdTicket.Seats.ShouldBe(new[] { "A1", "A2" });
        (await queries.GetTicketAsync(leaBird.Id, paul.Id)).ShouldBeNull();
    }
}
