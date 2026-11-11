namespace GrandCinema.Application.Features.Auth.DTOs;

public record class AuthTokenDto
{
    public required string AccessToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required UserDto User { get; init; }
}
