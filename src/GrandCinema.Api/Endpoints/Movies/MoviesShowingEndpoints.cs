using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Movies.DTOs;
using GrandCinema.Application.Features.Movies.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Movies;

public static class MoviesShowingEndpoints
{
    public static IEndpointRouteBuilder MapMoviesShowingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/movies", GetMoviesShowing)
            .AllowAnonymous()
            .WithName("GetMoviesShowing")
            .WithTags("Movies")
            .WithSummary("Movies currently showing")
            .WithDescription("Returns the movies with at least one upcoming screening, sorted by title.");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<MovieDto>>> GetMoviesShowing(
        IQueryHandler<GetMoviesShowingQuery, IReadOnlyList<MovieDto>> handler,
        CancellationToken cancellationToken)
    {
        var movies = await handler.Handle(new GetMoviesShowingQuery(), cancellationToken);
        return TypedResults.Ok(movies);
    }
}
