namespace GrandCinema.Application.Features.Programming.DTOs;

public record class ProgrammedScreeningDto
{
    public required Guid ScreeningId { get; init; }
    public required Guid MovieId { get; init; }
    public required string MovieTitle { get; init; }
    public required Guid RoomId { get; init; }
    public required string RoomName { get; init; }
    public required DateTime StartsAt { get; init; }
    public required decimal Price { get; init; }
    // Seats held (not expired) or sold: above zero, a change needs the confirmation of the manager
    public required int TakenSeats { get; init; }
    public required int Capacity { get; init; }
}
