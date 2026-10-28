namespace GrandCinema.Application.Features.Auth.Commands;

public sealed record class RegisterUserCommand(string Email, string Password, string FirstName, string LastName, bool AcceptPrivacyPolicy);
