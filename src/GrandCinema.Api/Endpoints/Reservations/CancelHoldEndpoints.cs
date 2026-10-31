using System.Security.Claims;
using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.Commands;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Reservations;

public static class CancelHoldEndpoints
{
    public static IEndpointRouteBuilder MapCancelHoldEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/reservations/{reservationId:guid}", CancelHold)
            .RequireAuthorization()
            .WithName("CancelHold")
            .WithTags("Reservations")
            .WithSummary("Cancel my hold before paying")
            .WithDescription("Cancels a hold of the signed-in customer before payment: its seats are on sale again at once. Nothing changes for a hold that has already expired or been cancelled (204). 404 for an unknown reservation and for the reservation of another customer; 409 reservation-confirmed for a paid reservation.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelHold(
        Guid reservationId,
        ClaimsPrincipal user,
        ICommandHandler<CancelHoldCommand, Result<Guid>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CancelHoldCommand(user.GetUserId(), reservationId), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.NoContent();
    }
}
