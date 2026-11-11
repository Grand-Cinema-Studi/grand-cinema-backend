namespace GrandCinema.Application.Features.Reservations.Commands;

public sealed record class HoldSeatsCommand(Guid UserId, Guid ScreeningId, int SeatCount);
