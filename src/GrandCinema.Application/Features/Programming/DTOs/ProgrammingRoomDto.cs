namespace GrandCinema.Application.Features.Programming.DTOs;

public record class ProgrammingRoomDto
{
    public required Guid RoomId { get; init; }
    public required string Name { get; init; }
    public required int Capacity { get; init; }
}
