using GrandCinema.Application.Abstractions.Security;
using GrandCinema.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GrandCinema.Infrastructure.Security;

// Adapter around ASP.NET Core Identity's PasswordHasher: PBKDF2 with HMAC-SHA512, a random 128-bit salt per password
// and 100,000 iterations by default. The algorithm and its settings are stored in the hash itself, so they can be raised
// later without invalidating existing passwords.
public sealed class AspNetCorePasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    // The default implementation does not use the user instance, hence null.
    public string Hash(string password) => _passwordHasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _passwordHasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
