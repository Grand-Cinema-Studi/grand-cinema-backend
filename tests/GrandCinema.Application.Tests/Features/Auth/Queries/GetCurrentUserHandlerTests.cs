using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Auth;
using GrandCinema.Application.Features.Auth.Queries;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Auth.Queries;

public class GetCurrentUserHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_signed_in_user_exists_Then_their_profile_is_returned()
    {
        // Arrange
        var lea = new User
        {
            Id = Guid.NewGuid(),
            Email = "lea.martin@example.com",
            PasswordHash = "hashed-password",
            FirstName = "Léa",
            LastName = "Martin",
            Role = UserRole.Customer
        };
        _users.Setup(r => r.GetByIdAsync(lea.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lea);
        var handler = new GetCurrentUserHandler(_users.Object);

        // Act
        var result = await handler.Handle(new GetCurrentUserQuery(lea.Id));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe("lea.martin@example.com");
        result.Value.Role.ShouldBe("Customer");
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_account_no_longer_exists_Then_user_not_found_is_returned()
    {
        // Arrange
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var handler = new GetCurrentUserHandler(_users.Object);

        // Act
        var result = await handler.Handle(new GetCurrentUserQuery(Guid.NewGuid()));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AuthErrors.UserNotFound);
    }
}
