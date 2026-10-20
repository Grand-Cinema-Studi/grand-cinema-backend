using GrandCinema.Infrastructure.Security;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Security;

public class AspNetCorePasswordHasherTests
{
    private const string Password = "festival-jazz-2026";
    private readonly AspNetCorePasswordHasher _hasher = new();

    [Fact]
    [Trait("US", "2.1")]
    public void When_a_password_is_hashed_Then_the_hash_does_not_contain_the_password()
    {
        // Act
        var hash = _hasher.Hash(Password);

        // Assert
        hash.ShouldNotBeNullOrWhiteSpace();
        hash.ShouldNotContain(Password);
    }

    [Fact]
    [Trait("US", "2.1")]
    public void When_the_same_password_is_hashed_twice_Then_the_hashes_differ_because_they_are_salted()
    {
        // Act
        var first = _hasher.Hash(Password);
        var second = _hasher.Hash(Password);

        // Assert
        first.ShouldNotBe(second);
    }

    [Fact]
    [Trait("US", "2.1")]
    public void When_the_right_password_is_given_Then_it_matches_its_hash()
    {
        // Arrange
        var hash = _hasher.Hash(Password);

        // Act
        var verified = _hasher.Verify(hash, Password);

        // Assert
        verified.ShouldBeTrue();
    }

    [Fact]
    [Trait("US", "2.1")]
    public void When_a_wrong_password_is_given_Then_it_does_not_match_the_hash()
    {
        // Arrange
        var hash = _hasher.Hash(Password);

        // Act
        var verified = _hasher.Verify(hash, "wrong-password-123");

        // Assert
        verified.ShouldBeFalse();
    }
}
