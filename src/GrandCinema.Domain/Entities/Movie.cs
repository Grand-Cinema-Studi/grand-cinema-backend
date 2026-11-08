namespace GrandCinema.Domain.Entities;

public class Movie
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Synopsis { get; set; }
    public int DurationMinutes { get; set; }
    public required string Genre { get; set; }
    public DateOnly ReleaseDate { get; set; }
    public required string PosterUrl { get; set; }
    // An archived movie is no longer offered to the programming; it is never deleted (past screenings, sales, tickets). Instant in UTC
    public DateTime? ArchivedAt { get; set; }
}
