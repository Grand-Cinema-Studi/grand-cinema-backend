using GrandCinema.Application.Features.Reservations.DTOs;

namespace GrandCinema.Application.Features.Reservations.Queries;

// ADR 0004, the seats are counted by PostgreSQL and projected straight into DTOs
public interface IReservationTrackingQueries
{
    // The screenings of a day (started ones included, cancelled ones left out), by time then room (if 2 movies start at the same time),
    // with their seats counted by kind (online, at counter, held) at the instant nowUtc (holds expire)
    Task<IReadOnlyList<ScreeningTrackingDto>> GetScreeningsOfDayAsync(DateOnly date, DateTime nowUtc, CancellationToken cancellationToken = default);
}
