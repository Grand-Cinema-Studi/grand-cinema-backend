using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Security;
using GrandCinema.Application.Features.Auth;
using GrandCinema.Application.Features.Auth.Commands;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Auth.Commands;

public class AuthenticateUserHandlerTests
{
    private const string Password = "festival-jazz-2026";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAccessTokenGenerator> _tokenGenerator = new();
    private readonly User _lea = new()
    {
        Id = Guid.NewGuid(),
        Email = "lea.martin@example.com",
        PasswordHash = "hashed-password",
        FirstName = "Léa",
        LastName = "Martin",
        Role = UserRole.Customer
    };

    public AuthenticateUserHandlerTests()
    {
        _users
            .Setup(r => r.GetByEmailAsync(_lea.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lea);
        _passwordHasher.Setup(h => h.Verify("hashed-password", Password)).Returns(true);
        _tokenGenerator
            .Setup(g => g.Generate(_lea))
            .Returns(new AccessToken("access-token", new DateTime(2026, 7, 18, 11, 0, 0, DateTimeKind.Utc)));
    }

    private AuthenticateUserHandler CreateHandler() =>
        new(_users.Object, _passwordHasher.Object, _tokenGenerator.Object);

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_credentials_are_correct_Then_the_user_is_logged_in()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new AuthenticateUserCommand("lea.martin@example.com", Password));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.User.UserId.ShouldBe(_lea.Id);
        result.Value.User.Role.ShouldBe("Customer");
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_password_is_wrong_Then_the_login_is_refused_without_saying_which_credential_is_wrong()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new AuthenticateUserCommand("lea.martin@example.com", "wrong-password-123"));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
        _tokenGenerator.Verify(g => g.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_email_is_unknown_Then_the_login_is_refused_with_the_same_message_as_a_wrong_password()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new AuthenticateUserCommand("unknown@example.com", Password));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_email_is_typed_with_uppercase_letters_Then_the_account_is_found()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new AuthenticateUserCommand(" Lea.Martin@Example.com", Password));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("US", "2.1")]
    [InlineData("", Password)]
    [InlineData("   ", Password)]
    [InlineData("lea.martin@example.com", "")]
    public async Task When_the_email_or_the_password_is_empty_Then_the_login_is_refused_without_querying_the_accounts(string email, string password)
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new AuthenticateUserCommand(email, password));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
        _users.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
