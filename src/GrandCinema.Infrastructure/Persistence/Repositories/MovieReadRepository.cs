using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public class MovieReadRepository : IMovieReadRepository
{
    private readonly GrandCinemaDbContext _db;

    public MovieReadRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<Movie>> GetMoviesScheduledFromAsync(DateTime from, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
