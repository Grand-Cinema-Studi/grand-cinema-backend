using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Programming;
using GrandCinema.Application.Features.Programming.Commands;
using GrandCinema.Application.Features.Programming.DTOs;
using GrandCinema.Application.Features.Programming.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GrandCinema.Api.Endpoints.Programming;

// The bodies of the requests: what the Responsable Programmation chooses (StartsAt: local time of the cinema)
public sealed record class CreateScreeningRequest(Guid MovieId, Guid RoomId, DateTime StartsAt, decimal Price);

public sealed record class UpdateScreeningRequest(DateTime StartsAt, decimal Price);

public sealed record class ScreeningCreatedResponse(Guid ScreeningId);

// The form of a new movie (multipart/form-data), text fields and the poster file. Every field is checked by the handler
public sealed class MovieForm
{
    public string? Title { get; set; }
    public string? Synopsis { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Genre { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public IFormFile Poster { get; set; } = null!;
}

public sealed record class MovieCreatedResponse(Guid MovieId);

public static class ProgrammingEndpoints
{
    private const string Confirmation =
        " When seats are held or sold: 409 screening-has-reservations, until the same request is sent again with ?confirmed=true.";

    public static IEndpointRouteBuilder MapProgrammingEndpoints(this IEndpointRouteBuilder app)
    {
        // The whole group is reserved to the manager; any other role receives 403, a visitor 401
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

        programming.MapPost("/movies", CreateMovie)
            .WithName("CreateMovie")
            .WithSummary("Add a movie to the catalog")
            .WithDescription("multipart/form-data: title, synopsis, durationMinutes, genre, releaseDate (e.g. 1988-09-30) and poster (JPEG, PNG or WebP, 2 MB at most, recognised from its first bytes). The movie can then be programmed; the public pages show it from its first screening. 409 movie-already-exists for the same title and release date.")
            .DisableAntiforgery()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
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

    private static async Task<Results<Created<MovieCreatedResponse>, ProblemHttpResult>> CreateMovie(
        [FromForm] MovieForm form,
        ICommandHandler<CreateMovieCommand, Result<Guid>> handler,
        CancellationToken cancellationToken)
    {
        var poster = await ReadPosterAsync(form.Poster, cancellationToken);
        var result = await handler.Handle(new CreateMovieCommand(form.Title, form.Synopsis, form.DurationMinutes, form.Genre, form.ReleaseDate, poster), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Created($"/api/movies/{result.Value}", new MovieCreatedResponse(result.Value));
    }

    private static async Task<byte[]?> ReadPosterAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return null;
        }

        var content = new byte[Math.Min(file.Length, PosterRules.MaxBytes + 1)];
        await using var stream = file.OpenReadStream();
        await stream.ReadExactlyAsync(content, cancellationToken);
        return content;
    }
}
