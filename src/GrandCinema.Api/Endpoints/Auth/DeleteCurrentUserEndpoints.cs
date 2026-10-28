using System.Security.Claims;
using GrandCinema.Api.Errors;
using GrandCinema.Api.Security;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Auth;

public static class DeleteCurrentUserEndpoints
{
    public static IEndpointRouteBuilder MapDeleteCurrentUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/users/me", DeleteCurrentUser)
            .RequireAuthorization()
            .WithName("DeleteCurrentUser")
            .WithTags("Auth")
            .WithSummary("Delete my account")
            .WithDescription("Deletes the account of the signed-in user (GDPR right to erasure). Its reservations are kept for accounting, without any link to the account. The access token stays valid until it expires, but the profile then answers 404.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteCurrentUser(
        ClaimsPrincipal user,
        ICommandHandler<DeleteCurrentUserCommand, Result<Guid>> handler,
        CancellationToken cancellationToken)
    {
        // The account to delete comes from the signed token, never from the URL: nobody can delete someone else's account.
        var result = await handler.Handle(new DeleteCurrentUserCommand(user.GetUserId()), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.NoContent();
    }
}
