using GrandCinema.Application.Abstractions.Persistence;

namespace GrandCinema.Application.Features.Programming;

public static class ScreeningReadRepositoryExtensions
{
    // Seats held (not expired) or sold, counted as the remaining seats of the schedule (US 2.2):
    // above zero, the manager is warned before a change (US 5.2, criterion 3)
    public static async Task<bool> HasTakenSeatsAsync(this IScreeningReadRepository screenings, Guid screeningId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var takenSeats = await screenings.CountTakenSeatsAsync([screeningId], nowUtc, cancellationToken);
        return takenSeats.GetValueOrDefault(screeningId) > 0;
    }
}
