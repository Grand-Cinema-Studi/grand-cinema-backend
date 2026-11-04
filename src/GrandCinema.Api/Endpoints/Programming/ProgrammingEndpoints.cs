using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Programming.Commands;
using GrandCinema.Application.Features.Programming.DTOs;
using GrandCinema.Application.Features.Programming.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Programming;

// The bodies of the requests: what the Responsable Programmation chooses (StartsAt: local time of the cinema)
public sealed record class CreateScreeningRequest(Guid MovieId, Guid RoomId, DateTime StartsAt, decimal Price);

public sealed record class UpdateScreeningRequest(DateTime StartsAt, decimal Price);

public sealed record class ScreeningCreatedResponse(Guid ScreeningId);

public static class ProgrammingEndpoints
{
    private const string Confirmation =
        " When seats are held or sold: 409 screening-has-reservations, until the same request is sent again with ?confirmed=true.";

    public static IEndpointRouteBuilder MapProgrammingEndpoints(this IEndpointRouteBuilder app)
    {
        // Criterion 4: the whole group is reserved to the manager; any other role receives 403, a visitor 401
        var programming = app.MapGroup("/api/programming")
            .RequireAuthorization(AuthenticationSetup.ManagerPolicy)
            .WithTags("Programming");

        programming.MapGet("", GetProgramming)
            .WithName("GetProgramming")
            .WithSummary("The programming of a day, for the Responsable Programmation")
            .WithDescription("Every screening of the day (today if the date is omitted), started ones included, with its seats held or sold and the capacity of its room; the whole catalog of movies and the rooms, for the form.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        programming.MapPost("/screenings", CreateScreening)
            .WithName("CreateScreening")
            .WithSummary("Create a screening")
            .WithDescription("Creates a screening of a movie in a room, at a future date and time (local time of the cinema) and a positive price; it appears at once in the public schedule. 409 room-not-available when another screening needs the room at that time, cleaning (15 minutes) included. 404 movie-not-found, room-not-found, written without time zone, e.g. 2026-11-30T20:30:00 (400 with \"Z\" or an offset).")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        programming.MapPut("/screenings/{screeningId:guid}", UpdateScreening)
            .WithName("UpdateScreening")
            .WithSummary("Move a screening or change its price")
            .WithDescription("Changes the date, the time and the price of a screening that has not started (409 screening-started). The room and the movie do not change: cancel, then create. The price of the seats already held or sold does not change. 409 room-not-available." + Confirmation)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        programming.MapDelete("/screenings/{screeningId:guid}", CancelScreening)
            .WithName("CancelScreening")
            .WithSummary("Cancel a screening")
            .WithDescription("The screening leaves the public schedule at once; its holds are cancelled and their seats freed; its sales are kept, for the history and the refunds. 409 screening-started." + Confirmation)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<ProgrammingDto>> GetProgramming(
        DateOnly? date,
        IQueryHandler<GetProgrammingQuery, ProgrammingDto> handler,
        CancellationToken cancellationToken)
    {
        var programming = await handler.Handle(new GetProgrammingQuery(date), cancellationToken);
        return TypedResults.Ok(programming);
    }

    private static async Task<Results<Created<ScreeningCreatedResponse>, ProblemHttpResult>> CreateScreening(
        CreateScreeningRequest request,
        ICommandHandler<CreateScreeningCommand, Result<Guid>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CreateScreeningCommand(request.MovieId, request.RoomId, request.StartsAt, request.Price), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Created($"/api/programming/screenings/{result.Value}", new ScreeningCreatedResponse(result.Value));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateScreening(
        Guid screeningId,
        UpdateScreeningRequest request,
        ICommandHandler<UpdateScreeningCommand, Result<Guid>> handler,
        CancellationToken cancellationToken,
        bool confirmed = false)
    {
        var result = await handler.Handle(new UpdateScreeningCommand(screeningId, request.StartsAt, request.Price, confirmed), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelScreening(
        Guid screeningId,
        ICommandHandler<CancelScreeningCommand, Result<Guid>> handler,
        CancellationToken cancellationToken,
        bool confirmed = false)
    {
        var result = await handler.Handle(new CancelScreeningCommand(screeningId, confirmed), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.NoContent();
    }
}
