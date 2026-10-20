using System.Security.Claims;
using GrandCinema.Infrastructure.Security;

namespace GrandCinema.Api.Security;

internal static class ClaimsPrincipalExtensions
{
    // The user id always comes from the signed token, never from the URL or the request body.
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(AppClaimTypes.UserId)?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated user has no valid identifier.");
}
