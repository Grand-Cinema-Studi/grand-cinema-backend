namespace GrandCinema.Application.Features.Auth.DTOs;

public record class UserDto
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Role { get; init; }
}
