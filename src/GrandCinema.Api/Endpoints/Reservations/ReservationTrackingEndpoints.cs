using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Reservations;

public static class ReservationTrackingEndpoints
{
    public static IEndpointRouteBuilder MapReservationTrackingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reservation-tracking", GetReservationTracking)
            .RequireAuthorization(AuthenticationSetup.ReservationTrackingPolicy)
            .WithName("GetReservationTracking")
            .WithTags("Reservations")
            .WithSummary("Reservations of each screening of a day")
            .WithDescription("For the manager and the cashier (read only): Every screening of the day (today if the date is omitted), started ones included, cancelled ones left out: seats sold online (paid on Stripe), sold at the counter, held by a payment in progress, capacity of the room and seats left.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<Ok<ReservationTrackingDto>> GetReservationTracking(
        DateOnly? date,
        IQueryHandler<GetReservationTrackingQuery, ReservationTrackingDto> handler,
        CancellationToken cancellationToken)
    {
        var tracking = await handler.Handle(new GetReservationTrackingQuery(date), cancellationToken);
        return TypedResults.Ok(tracking);
    }
}
