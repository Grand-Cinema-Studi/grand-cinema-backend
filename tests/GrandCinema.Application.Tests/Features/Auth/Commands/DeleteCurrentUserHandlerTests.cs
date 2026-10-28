using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Auth;
using GrandCinema.Application.Features.Auth.Commands;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Auth.Commands;

public class DeleteCurrentUserHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_signed_in_user_deletes_their_account_Then_this_account_is_deleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _users.Setup(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new DeleteCurrentUserHandler(_users.Object);

        // Act
        var result = await handler.Handle(new DeleteCurrentUserCommand(userId));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _users.Verify(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("US", "2.1")]
    public async Task When_the_account_no_longer_exists_Then_user_not_found_is_returned()
    {
        // Arrange
        _users.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = new DeleteCurrentUserHandler(_users.Object);

        // Act
        var result = await handler.Handle(new DeleteCurrentUserCommand(Guid.NewGuid()));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AuthErrors.UserNotFound);
    }
}
