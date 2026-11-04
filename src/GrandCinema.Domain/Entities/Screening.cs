namespace GrandCinema.Domain.Entities;

public class Screening
{
    // Time to empty and clean the room before the next screening
    public static readonly TimeSpan CleaningTime = TimeSpan.FromMinutes(15);

    public Guid Id { get; set; }
    public Room Room { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public decimal Price { get; set; }

    // A cancelled screening leaves the schedule, its reservations are kept (history, refunds); instant in UTC
    public DateTime? CancelledAt { get; set; }

    public Guid MovieId { get; set; }
    public Guid RoomId { get; set; }

    /// <summary>
    /// True when a screening of durationMinutes starting at startsAt would need the room while this one does:
    /// from its start to the end of its movie, cleaning included. Needs Movie.
    /// </summary>
    public bool OverlapsWith(DateTime startsAt, int durationMinutes) =>
        startsAt < StartsAt + TimeSpan.FromMinutes(Movie.DurationMinutes) + CleaningTime
        && StartsAt < startsAt + TimeSpan.FromMinutes(durationMinutes) + CleaningTime;
}
