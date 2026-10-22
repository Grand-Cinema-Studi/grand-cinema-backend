using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class MovieReadRepository : IMovieReadRepository
{
    private readonly GrandCinemaDbContext _db;

    public MovieReadRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Movie>> GetMoviesScheduledFromAsync(DateTime from, CancellationToken cancellationToken = default)
    {
        return await _db.Movies
            .AsNoTracking()
            .Where(m => _db.Screenings.Any(s => s.MovieId == m.Id && s.StartsAt >= from))
            .ToListAsync(cancellationToken);
    }
}
