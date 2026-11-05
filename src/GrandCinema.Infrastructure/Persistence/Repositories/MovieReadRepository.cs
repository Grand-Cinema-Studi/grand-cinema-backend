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

    public async Task<Movie?> GetByIdAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        return await _db.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == movieId, cancellationToken);
    }

    public async Task<IReadOnlyList<Movie>> GetMoviesByNextScreeningAsync(DateTime from, int count, CancellationToken cancellationToken = default)
    {
        // A screening that has started does not count (StartsAt > from), a cancelled one is hidden by the global filter
        return await _db.Movies
            .AsNoTracking()
            .Where(m => _db.Screenings.Any(s => s.MovieId == m.Id && s.StartsAt > from))
            .OrderBy(m => _db.Screenings.Where(s => s.MovieId == m.Id && s.StartsAt > from).Min(s => s.StartsAt))
            .ThenBy(m => m.Title)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
