using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Programming.DTOs;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class ProgrammingRepository : IProgrammingRepository
{
    private readonly GrandCinemaDbContext _db;

    public ProgrammingRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<Movie>> GetMoviesAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<Room>> GetRoomsAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
    public Task<Room?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
    public Task<Screening?> GetScreeningAsync(Guid screeningId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
    public Task<IReadOnlyList<Screening>> GetScreeningsOfRoomAsync(Guid roomId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
    public Task AddScreeningAsync(Screening screening, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
    public Task UpdateScreeningAsync(Guid screeningId, DateTime startsAt, decimal price, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
    public Task CancelScreeningAsync(Guid screeningId, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
