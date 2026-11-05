using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;

namespace GrandCinema.Infrastructure.Persistence.Queries;

public sealed class ReservationTrackingQueries : IReservationTrackingQueries
{
    private readonly GrandCinemaDbContext _db;

    public ReservationTrackingQueries(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<ScreeningTrackingDto>> GetScreeningsOfDayAsync(DateOnly date, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
