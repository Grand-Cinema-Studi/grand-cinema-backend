using System.Security.Claims;
using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Auth.DTOs;
using GrandCinema.Application.Features.Auth.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Auth;

public static class CurrentUserEndpoints
{
    public static IEndpointRouteBuilder MapCurrentUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/me", GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithTags("Auth")
            .WithSummary("Profile of the signed-in user")
            .WithDescription("Returns the account of the user identified by the access token. Without a valid token, the answer is 401.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Ok<UserDto>, ProblemHttpResult>> GetCurrentUser(
        ClaimsPrincipal user,
        IQueryHandler<GetCurrentUserQuery, Result<UserDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCurrentUserQuery(user.GetUserId()), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
