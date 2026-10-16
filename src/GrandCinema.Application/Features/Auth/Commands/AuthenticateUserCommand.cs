namespace GrandCinema.Application.Features.Auth.Commands;

public sealed record class AuthenticateUserCommand(string Email, string Password);
