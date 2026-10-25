namespace GrandCinema.Domain.Entities;

public class Screening
{
    public Guid Id { get; set; }
    public Room Room { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public decimal Price { get; set; }

    public Guid MovieId { get; set; }
    public Guid RoomId { get; set; }
}
