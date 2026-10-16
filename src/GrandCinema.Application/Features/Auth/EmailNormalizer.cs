namespace GrandCinema.Application.Features.Auth;

internal static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
