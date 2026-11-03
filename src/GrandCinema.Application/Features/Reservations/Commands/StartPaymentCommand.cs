namespace GrandCinema.Application.Features.Reservations.Commands;

public record class StartPaymentCommand(Guid UserId, Guid ReservationId, bool AcceptTermsOfSale);
