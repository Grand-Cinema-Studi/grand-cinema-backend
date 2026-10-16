using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Auth.Commands;
using GrandCinema.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Auth;

public static class AuthenticateUserEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticateUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tokens", AuthenticateUser)
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationSetup.AuthenticationRateLimitingPolicy)
            .WithName("AuthenticateUser")
            .WithTags("Auth")
            .WithSummary("Sign in")
            .WithDescription("Checks the email address and the password and returns an access token (JWT). A failure never says which of the two is wrong.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<Results<Ok<AuthTokenDto>, ProblemHttpResult>> AuthenticateUser(
        AuthenticateUserCommand command,
        ICommandHandler<AuthenticateUserCommand, Result<AuthTokenDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
