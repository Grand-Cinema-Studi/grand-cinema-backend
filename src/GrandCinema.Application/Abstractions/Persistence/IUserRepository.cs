using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Abstractions.Persistence;

public interface IUserRepository
{
    /// <summary>Saves a new user. Returns false if the email address is already used by another account.</summary>
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
