using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly GrandCinemaDbContext _db;

    public UserRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken = default)
    {
        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        // The unique index on Email is the real guarantee: two sign-ups with the same address at the same instant
        // both pass any prior check, but PostgreSQL accepts only one of them.
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _db.Entry(user).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
