using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Abstractions.Persistence;

public interface IScreeningReadRepository
{
    Task<IReadOnlyList<Screening>> GetScreeningsAsync(DateOnly date, CancellationToken cancellationToken = default);

    // Number of seats taken per screening: sold, or held by a payment whose expiry is after `now` (UTC).
    // A screening without any seat taken is absent from the result.
    Task<IReadOnlyDictionary<Guid, int>> CountTakenSeatsAsync(IReadOnlyCollection<Guid> screeningIds, DateTime now, CancellationToken cancellationToken = default);
    // Screenings of a movie starting at `from` or later (local time of the cinema), with their room, without any date limit.
    Task<IReadOnlyList<Screening>> GetUpcomingScreeningsOfMovieAsync(Guid movieId, DateTime from, CancellationToken cancellationToken = default);

    Task<Screening?> GetByIdAsync(Guid screeningId, CancellationToken cancellationToken = default);
}
