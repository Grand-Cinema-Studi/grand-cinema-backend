using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.Commands;
using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Reservations;

public sealed record class CheckTicketRequest(Guid ScreeningId, string? TicketCode);

public static class TicketCheckEndpoints
{
    public static IEndpointRouteBuilder MapTicketCheckEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ticket-checks")
            .RequireAuthorization(AuthenticationSetup.TicketInspectionPolicy)
            .WithTags("Ticket checks");

        group.MapGet("/screenings", GetScreenings)
            .WithName("GetTicketCheckScreenings")
            .WithSummary("The screenings the inspector can control")
            .WithDescription("For the inspector: the screenings of a day (today, in the time of the cinema, if the date is omitted), started ones included, cancelled ones excluded, by time then room.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/", CheckTicket)
            .WithName("CheckTicket")
            .WithSummary("Check a ticket at the door of a screening")
            .WithDescription("For the inspector: a paid ticket of this screening, not used yet, lets its holders in (200, movie and seats) and is marked used, in one statement. 409 ticket-already-used, 409 ticket-for-another-screening, 404 ticket-not-found (unknown code, or a QR code of something else).")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<TicketCheckScreeningDto>>> GetScreenings(
        DateOnly? date,
        IQueryHandler<GetTicketCheckScreeningsQuery, IReadOnlyList<TicketCheckScreeningDto>> handler,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await handler.Handle(new GetTicketCheckScreeningsQuery(date), cancellationToken));
    }

    private static async Task<Results<Ok<TicketCheckDto>, ProblemHttpResult>> CheckTicket(
        CheckTicketRequest request,
        ICommandHandler<CheckTicketCommand, Result<TicketCheckDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CheckTicketCommand(request.ScreeningId, request.TicketCode), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
