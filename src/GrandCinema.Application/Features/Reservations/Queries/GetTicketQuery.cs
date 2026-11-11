namespace GrandCinema.Application.Features.Reservations.Queries;

public record class GetTicketQuery(Guid UserId, Guid ReservationId);
