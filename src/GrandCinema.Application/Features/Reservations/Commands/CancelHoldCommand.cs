namespace GrandCinema.Application.Features.Reservations.Commands;

public record class CancelHoldCommand(Guid UserId, Guid ReservationId);
