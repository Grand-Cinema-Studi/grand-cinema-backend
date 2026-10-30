using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Abstractions.Persistence;

public interface IMovieReadRepository
{
    Task<IReadOnlyList<Movie>> GetMoviesScheduledFromAsync(DateTime from, CancellationToken cancellationToken = default);
    Task<Movie?> GetByIdAsync(Guid movieId, CancellationToken cancellationToken = default);
}
