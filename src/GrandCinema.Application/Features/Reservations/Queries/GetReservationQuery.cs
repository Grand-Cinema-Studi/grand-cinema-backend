namespace GrandCinema.Application.Features.Reservations.Queries;

public sealed record class GetReservationQuery(Guid UserId, Guid ReservationId);
