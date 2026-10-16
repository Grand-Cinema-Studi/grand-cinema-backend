namespace GrandCinema.Application.Abstractions.Security;

public sealed record class AccessToken(string Value, DateTime ExpiresAt);
