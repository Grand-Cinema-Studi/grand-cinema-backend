using System.Security.Claims;
using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.Commands;
using GrandCinema.Application.Features.Reservations.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Reservations;

// The body of the request: US 3.1, criterion 4
public sealed record class StartPaymentRequest(bool AcceptTermsOfSale);

public static class StartPaymentEndpoints
{
    public static IEndpointRouteBuilder MapStartPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reservations/{reservationId:guid}/payment", StartPayment)
            .RequireAuthorization()
            .WithName("StartPayment")
            .WithTags("Reservations")
            .WithSummary("Open the payment page of my hold")
            .WithDescription("Records the acceptance of the terms of sale (acceptTermsOfSale must be true) and creates the Stripe payment page of a hold of the signed-in customer; the card is typed on Stripe's page, never on ours. 400 when the terms of sale are not accepted; 404 for an unknown reservation and for the reservation of another customer; 409 reservation-paid; 409 hold-expired once the 10 minutes have run out or after a cancel.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<PaymentPageDto>, ProblemHttpResult>> StartPayment(
        Guid reservationId,
        StartPaymentRequest request,
        ClaimsPrincipal user,
        ICommandHandler<StartPaymentCommand, Result<PaymentPageDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new StartPaymentCommand(user.GetUserId(), reservationId, request.AcceptTermsOfSale), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
