using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Auth.Commands;
using GrandCinema.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Auth;

public static class RegisterUserEndpoints
{
    public static IEndpointRouteBuilder MapRegisterUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users", RegisterUser)
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationSetup.AuthenticationRateLimitingPolicy)
            .WithName("RegisterUser")
            .WithTags("Auth")
            .WithSummary("Create a customer account")
            .WithDescription("Creates a customer account and signs the user in: the response contains an access token (JWT). The email address must not be used by another account; the password must contain at least 12 characters.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Created<AuthTokenDto>, ProblemHttpResult>> RegisterUser(
        RegisterUserCommand command,
        ICommandHandler<RegisterUserCommand, Result<AuthTokenDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Created("/api/users/me", result.Value);
    }
}
