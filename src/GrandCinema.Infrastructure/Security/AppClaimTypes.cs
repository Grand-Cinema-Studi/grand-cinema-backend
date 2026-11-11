namespace GrandCinema.Infrastructure.Security;

// Standard JWT claim names (RFC 7519 and OpenID Connect), shared by the token generator and the API.
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string TokenId = "jti";
}
