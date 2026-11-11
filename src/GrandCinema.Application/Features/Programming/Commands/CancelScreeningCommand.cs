namespace GrandCinema.Application.Features.Programming.Commands;

public sealed record class CancelScreeningCommand(Guid ScreeningId, bool Confirmed);
