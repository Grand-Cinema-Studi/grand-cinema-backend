namespace GrandCinema.Application.Features.Programming.Commands;

public sealed record class CreateScreeningCommand(Guid MovieId, Guid RoomId, DateTime StartsAt, decimal Price);
