namespace GrandCinema.Domain.Entities;

public class Room
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public int Capacity { get; set; }
}
