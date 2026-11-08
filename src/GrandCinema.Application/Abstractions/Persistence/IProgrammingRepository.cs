using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Abstractions.Persistence;

public interface IProgrammingRepository
{
    // The catalog without the archived movies, sorted by title: a new movie can be programmed before its first screening
    Task<IReadOnlyList<Movie>> GetMoviesAsync(CancellationToken cancellationToken = default);

    // Whatever the case of the title: "Bird" and "BIRD" released the same day are the same movie; an archived movie no longer counts
    Task<bool> MovieExistsAsync(string title, DateOnly releaseDate, CancellationToken cancellationToken = default);

    Task AddMovieAsync(Movie movie, CancellationToken cancellationToken = default);

    // As MovieExistsAsync, the movie itself left out: its own card can be saved again
    Task<bool> OtherMovieExistsAsync(Guid movieId, string title, DateOnly releaseDate, CancellationToken cancellationToken = default);

    Task UpdateMovieAsync(Movie movie, CancellationToken cancellationToken = default);

    // A screening of the movie, not cancelled, started after startedAfter: it has not ended when startedAfter is now minus the duration of the movie
    Task<bool> HasScreeningsNotEndedAsync(Guid movieId, DateTime startedAfter, CancellationToken cancellationToken = default);

    // Archiving twice keeps the first date
    Task ArchiveMovieAsync(Guid movieId, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> GetRoomsAsync(CancellationToken cancellationToken = default);

    Task<Room?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default);

    // With its movie, whose duration tells how long the room is busy
    Task<Screening?> GetScreeningAsync(Guid screeningId, CancellationToken cancellationToken = default);

    // The screenings of a room starting from `from` (included) to `to` (excluded), with their movie
    Task<IReadOnlyList<Screening>> GetScreeningsOfRoomAsync(Guid roomId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task AddScreeningAsync(Screening screening, CancellationToken cancellationToken = default);

    Task UpdateScreeningAsync(Guid screeningId, DateTime startsAt, decimal price, CancellationToken cancellationToken = default);

    // Under the lock of the screening: it leaves the schedule, its holds are cancelled and their seats freed, its sales are kept
    Task CancelScreeningAsync(Guid screeningId, DateTime nowUtc, CancellationToken cancellationToken = default);

}
