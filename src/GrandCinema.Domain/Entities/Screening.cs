namespace GrandCinema.Domain.Entities;

public class Screening
{
    public Guid Id { get; set; }
    public required Room Room { get; set; }
    public required Movie Movie { get; set; }
    public DateTime StartsAt { get; set; }
    public decimal Price { get; set; }
}
