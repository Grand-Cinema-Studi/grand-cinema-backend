using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Movies.DTOs;
using GrandCinema.Application.Features.Movies.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Movies;

public static class FeaturedMoviesEndpoints
{
    public static IEndpointRouteBuilder MapFeaturedMoviesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/movies/featured", GetFeaturedMovies)
            .AllowAnonymous()
            .WithName("GetFeaturedMovies")
            .WithTags("Movies")
            .WithSummary("Featured movies of the home page")
            .WithDescription("Returns at most four movies, those whose next screening comes first: today's movies, then those of the following days and months. A screening that has started, or a cancelled one, does not count.");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<MovieDto>>> GetFeaturedMovies(
        IQueryHandler<GetFeaturedMoviesQuery, IReadOnlyList<MovieDto>> handler,
        CancellationToken cancellationToken)
    {
        var movies = await handler.Handle(new GetFeaturedMoviesQuery(), cancellationToken);
        return TypedResults.Ok(movies);
    }
}
