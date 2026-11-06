using System.Security.Claims;
using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Reservations;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reservations/{reservationId:guid}/ticket", GetTicket)
            .RequireAuthorization()
            .WithName("GetTicket")
            .WithTags("Reservations")
            .WithSummary("The ticket of one of my paid reservations")
            .WithDescription("Movie, screening, room, seats and the ticket code, drawn when the payment was confirmed; the front shows the code as a QR code. 404 ticket-not-found for a reservation that is not paid, unknown, or of another customer.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet("/api/users/me/tickets", GetMyTickets)
            .RequireAuthorization()
            .WithName("GetMyTickets")
            .WithTags("Reservations")
            .WithSummary("My tickets")
            .WithDescription("The paid reservations of the signed-in customer, the latest screening first; a screening cancelled after the payment is flagged.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<Results<Ok<TicketDto>, ProblemHttpResult>> GetTicket(
        Guid reservationId,
        ClaimsPrincipal user,
        IQueryHandler<GetTicketQuery, Result<TicketDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetTicketQuery(user.GetUserId(), reservationId), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<IReadOnlyList<TicketSummaryDto>>> GetMyTickets(
        ClaimsPrincipal user,
        IQueryHandler<GetMyTicketsQuery, IReadOnlyList<TicketSummaryDto>> handler,
        CancellationToken cancellationToken)
    {
        var tickets = await handler.Handle(new GetMyTicketsQuery(user.GetUserId()), cancellationToken);
        return TypedResults.Ok(tickets);
    }
}
