using GrandCinema.Api.Errors;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Movies.DTOs;
using GrandCinema.Application.Features.Movies.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Movies;

public static class MovieDetailsEndpoints
{
    public static IEndpointRouteBuilder MapMovieDetailsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/movies/{movieId:guid}", GetMovieDetails)
            .AllowAnonymous()
            .WithName("GetMovieDetails")
            .WithTags("Movies")
            .WithSummary("A movie and its upcoming screenings")
            .WithDescription("Returns the movie and all its upcoming screenings, grouped by day and sorted by time, with the remaining seats of each screening. 404 if the movie does not exist.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Ok<MovieDetailsDto>, ProblemHttpResult>> GetMovieDetails(
        Guid movieId,
        IQueryHandler<GetMovieDetailsQuery, Result<MovieDetailsDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetMovieDetailsQuery(movieId), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
