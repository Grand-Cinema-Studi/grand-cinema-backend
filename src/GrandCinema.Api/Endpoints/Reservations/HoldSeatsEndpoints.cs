using System.Security.Claims;
using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.Commands;
using GrandCinema.Application.Features.Reservations.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Reservations;

// The body of the request: the customer comes from the signed token, never from the body.
public sealed record class HoldSeatsRequest(Guid ScreeningId, int SeatCount);

public static class HoldSeatsEndpoints
{
    public static IEndpointRouteBuilder MapHoldSeatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reservations", HoldSeats)
            .RequireAuthorization()
            .WithName("HoldSeats")
            .WithTags("Reservations")
            .WithSummary("Hold seats for 10 minutes")
            .WithDescription("Holds the given number of seats (1 to 10) of a screening for the signed-in customer while they pay. A customer cannot have more than 10 seats, held or paid, for the same screening (409 seat-limit-reached). The seats are given by the cinema, row after row, and freed after 10 minutes without payment. 409 not-enough-seats when fewer seats are left, also for the second of two customers who want the last seat at the same time; 409 screening-started; 404 screening-not-found.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Created<ReservationDto>, ProblemHttpResult>> HoldSeats(
        HoldSeatsRequest request,
        ClaimsPrincipal user,
        ICommandHandler<HoldSeatsCommand, Result<ReservationDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new HoldSeatsCommand(user.GetUserId(), request.ScreeningId, request.SeatCount), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Created($"/api/reservations/{result.Value.ReservationId}", result.Value);
    }
}
