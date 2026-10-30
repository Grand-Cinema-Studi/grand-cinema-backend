using System.Security.Claims;
using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Reservations;

public static class GetReservationEndpoints
{
    public static IEndpointRouteBuilder MapGetReservationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reservations/{reservationId:guid}", GetReservation)
            .RequireAuthorization()
            .WithName("GetReservation")
            .WithTags("Reservations")
            .WithSummary("One of my reservations")
            .WithDescription("Returns a reservation of the signed-in customer with its seats and its status: PendingPayment, Expired once the 10 minutes have run out, Confirmed once paid. 404 for an unknown reservation and for the reservation of another customer.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Ok<ReservationDto>, ProblemHttpResult>> GetReservation(
        Guid reservationId,
        ClaimsPrincipal user,
        IQueryHandler<GetReservationQuery, Result<ReservationDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetReservationQuery(user.GetUserId(), reservationId), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
