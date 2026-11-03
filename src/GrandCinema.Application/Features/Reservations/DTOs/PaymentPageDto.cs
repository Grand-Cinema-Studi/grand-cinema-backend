namespace GrandCinema.Application.Features.Reservations.DTOs;

public record class PaymentPageDto
{
    // The page of Stripe where the card is typed: the front sends the customer there
    public required string CheckoutUrl { get; init; }
}
