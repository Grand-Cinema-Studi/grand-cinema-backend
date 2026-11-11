namespace GrandCinema.Application.Features.Programming.Commands;

public sealed record class UpdateScreeningCommand(Guid ScreeningId, DateTime StartsAt, decimal Price, bool Confirmed);
