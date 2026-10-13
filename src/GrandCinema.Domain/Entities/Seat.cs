namespace GrandCinema.Domain.Entities;

public class Seat
{
    public Guid Id { get; set; }
    public Room Room { get; set; } = null!;
    public Guid RoomId { get; set; }
    public required string Row { get; set; }
    public int Number { get; set; }
}
