using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using GrandCinema.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Persistence.Repositories;

public class UserRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _database;

    public UserRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    private static User NewUser(string email) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        PasswordHash = "hashed-password",
        FirstName = "Léa",
        LastName = "Martin",
        CreatedAt = DateTime.UtcNow,
        Role = UserRole.Customer
    };

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_user_is_added_Then_it_can_be_found_by_email_and_by_id()
    {
        // Arrange
        var user = NewUser($"{Guid.NewGuid()}@example.com");
        await using (var db = _database.CreateDbContext())
        {
            (await new UserRepository(db).TryAddAsync(user)).ShouldBeTrue();
        }

        await using var readDb = _database.CreateDbContext();
        var repository = new UserRepository(readDb);

        // Act
        var byEmail = await repository.GetByEmailAsync(user.Email);
        var byId = await repository.GetByIdAsync(user.Id);

        // Assert
        byEmail.ShouldNotBeNull();
        byEmail.Id.ShouldBe(user.Id);
        byId.ShouldNotBeNull();
        byId.Email.ShouldBe(user.Email);
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_email_is_already_used_Then_the_second_account_is_refused_by_the_database()
    {
        // Arrange
        var email = $"{Guid.NewGuid()}@example.com";
        await using (var db = _database.CreateDbContext())
        {
            await new UserRepository(db).TryAddAsync(NewUser(email));
        }

        await using var secondDb = _database.CreateDbContext();
        var repository = new UserRepository(secondDb);

        // Act
        var added = await repository.TryAddAsync(NewUser(email));

        // Assert
        added.ShouldBeFalse();
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_user_is_deleted_Then_they_can_no_longer_be_found()
    {
        // Arrange
        var user = NewUser($"{Guid.NewGuid()}@example.com");
        await using (var db = _database.CreateDbContext())
        {
            await new UserRepository(db).TryAddAsync(user);
        }

        await using var deleteDb = _database.CreateDbContext();
        var repository = new UserRepository(deleteDb);

        // Act
        var deleted = await repository.DeleteAsync(user.Id);

        // Assert
        deleted.ShouldBeTrue();
        (await repository.GetByIdAsync(user.Id)).ShouldBeNull();
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_user_does_not_exist_Then_nothing_is_deleted()
    {
        // Arrange
        await using var db = _database.CreateDbContext();
        var repository = new UserRepository(db);

        // Act
        var deleted = await repository.DeleteAsync(Guid.NewGuid());

        // Assert
        deleted.ShouldBeFalse();
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_user_with_a_reservation_is_deleted_Then_the_reservation_is_kept_without_any_link_to_them()
    {
        // Arrange: a paid reservation, which must be kept for accounting
        var user = NewUser($"{Guid.NewGuid()}@example.com");
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = 100 };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Kept reservation", Synopsis = "Synopsis", DurationMinutes = 100, Genre = "Drama", ReleaseDate = new DateOnly(2025, 1, 1), PosterUrl = "https://example.com/poster.jpg" };
        var screening = new Screening { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = new DateTime(2026, 7, 18, 20, 0, 0, DateTimeKind.Unspecified), Price = 9m };
        var reservation = new Reservation { Id = Guid.NewGuid(), User = user, Screening = screening, Status = ReservationStatus.Confirmed, CreatedAt = DateTime.UtcNow, TotalAmount = 18m };
        await using (var seed = _database.CreateDbContext())
        {
            seed.Reservations.Add(reservation);
            await seed.SaveChangesAsync();
        }

        // Act
        await using (var db = _database.CreateDbContext())
        {
            (await new UserRepository(db).DeleteAsync(user.Id)).ShouldBeTrue();
        }

        // Assert
        await using var readDb = _database.CreateDbContext();
        var kept = await readDb.Reservations.SingleAsync(r => r.Id == reservation.Id);
        kept.UserId.ShouldBeNull();
        kept.TotalAmount.ShouldBe(18m);
    }
}
