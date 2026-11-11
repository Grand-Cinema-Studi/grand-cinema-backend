namespace GrandCinema.Application.Features.Reservations.Commands;

public sealed record class CheckTicketCommand(Guid ScreeningId, string? TicketCode);
