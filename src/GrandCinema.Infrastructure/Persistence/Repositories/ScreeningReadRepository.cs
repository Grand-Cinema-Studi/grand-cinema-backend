using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class ScreeningReadRepository : IScreeningReadRepository
{
    private readonly GrandCinemaDbContext _db;

    public ScreeningReadRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<Screening>> GetScreeningsAsync(DateOnly date, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
