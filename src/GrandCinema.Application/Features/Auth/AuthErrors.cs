using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Auth;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyUsed =
        Error.Conflict("email-already-used", "An account already exists with this email address.");

    // Same error for an unknown email and a wrong password: the API never reveals which addresses have an account.
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("invalid-credentials", "The email address or the password is incorrect.");

    public static readonly Error UserNotFound =
        Error.NotFound("user-not-found", "The user account no longer exists.");
}
