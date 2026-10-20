using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using GrandCinema.Infrastructure.Persistence.Repositories;
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
}
