using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Abstractions.Security;
using GrandCinema.Application.Features.Auth;
using GrandCinema.Application.Features.Auth.Commands;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Auth.Commands;

public class RegisterUserHandlerTests
{
    private const string Password = "festival-jazz-2026";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAccessTokenGenerator> _tokenGenerator = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));
    private User? _savedUser;

    public RegisterUserHandlerTests()
    {
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _tokenGenerator
            .Setup(g => g.Generate(It.IsAny<User>()))
            .Returns(new AccessToken("access-token", new DateTime(2026, 7, 18, 11, 0, 0, DateTimeKind.Utc)));
        _users
            .Setup(r => r.TryAddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => _savedUser = user)
            .ReturnsAsync(true);
    }

    private RegisterUserHandler CreateHandler() =>
        new(_users.Object, _passwordHasher.Object, _tokenGenerator.Object, _time);

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_valid_information_is_entered_Then_the_account_is_created_and_the_user_is_logged_in()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new RegisterUserCommand("lea.martin@example.com", Password, "Léa", "Martin"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.User.Email.ShouldBe("lea.martin@example.com");
        _users.Verify(r => r.TryAddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_an_account_is_created_Then_its_role_is_customer()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await handler.Handle(new RegisterUserCommand("lea.martin@example.com", Password, "Léa", "Martin"));

        // Assert
        _savedUser.ShouldNotBeNull();
        _savedUser.Role.ShouldBe(UserRole.Customer);
        _savedUser.CreatedAt.ShouldBe(new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_email_is_already_used_Then_the_account_creation_is_refused_with_a_clear_message()
    {
        // Arrange
        _users
            .Setup(r => r.TryAddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new RegisterUserCommand("lea.martin@example.com", Password, "Léa", "Martin"));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AuthErrors.EmailAlreadyUsed);
        _tokenGenerator.Verify(g => g.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_an_account_is_created_Then_the_password_is_stored_hashed()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await handler.Handle(new RegisterUserCommand("lea.martin@example.com", Password, "Léa", "Martin"));

        // Assert
        _passwordHasher.Verify(h => h.Hash(Password), Times.Once);
        _savedUser.ShouldNotBeNull();
        _savedUser.PasswordHash.ShouldBe("hashed-password");
        _savedUser.PasswordHash.ShouldNotContain(Password);
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_email_has_uppercase_letters_or_spaces_Then_it_is_stored_in_lowercase_without_spaces()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await handler.Handle(new RegisterUserCommand("  Lea.Martin@Example.COM ", Password, "Léa", "Martin"));

        // Assert
        _savedUser.ShouldNotBeNull();
        _savedUser.Email.ShouldBe("lea.martin@example.com");
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_information_is_invalid_Then_the_account_creation_is_refused_and_each_invalid_field_is_listed()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new RegisterUserCommand("not-an-email", "too-short", " ", "Martin"));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull();
        result.Error.ValidationErrors.Keys.ShouldBe(new[] { "email", "password", "firstName" }, ignoreOrder: true);
        _users.Verify(r => r.TryAddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public static TheoryData<string, RegisterUserCommand> CommandsWithOneInvalidField => new()
    {
        { "email", new RegisterUserCommand(new string('a', 245) + "@example.com", Password, "Léa", "Martin") }, // 257 characters
        { "password", new RegisterUserCommand("lea.martin@example.com", new string('p', 129), "Léa", "Martin") },
        { "firstName", new RegisterUserCommand("lea.martin@example.com", Password, new string('L', 101), "Martin") },
        { "lastName", new RegisterUserCommand("lea.martin@example.com", Password, "Léa", new string('M', 101)) },
        { "lastName", new RegisterUserCommand("lea.martin@example.com", Password, "Léa", "   ") },
    };

    [Theory]
    [Trait("US", "2.1")]
    [MemberData(nameof(CommandsWithOneInvalidField))]
    public async Task When_one_field_is_missing_or_too_long_Then_the_account_creation_is_refused_for_this_field_only(string invalidField, RegisterUserCommand command)
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull();
        result.Error.ValidationErrors.Keys.ShouldBe([invalidField]);
        _users.Verify(r => r.TryAddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_each_field_has_its_maximum_length_Then_the_account_is_created()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new RegisterUserCommand(
            new string('a', 244) + "@example.com", // 256 characters
            new string('p', RegisterUserHandler.MaximumPasswordLength),
            new string('L', 100),
            new string('M', 100));

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _users.Verify(r => r.TryAddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
