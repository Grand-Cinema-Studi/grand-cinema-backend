using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Abstractions.Security;
using GrandCinema.Application.Features.Auth.DTOs;
using GrandCinema.Application.Features.Auth.DTOs.Mappings;

namespace GrandCinema.Application.Features.Auth.Commands;

public sealed class AuthenticateUserHandler : ICommandHandler<AuthenticateUserCommand, Result<AuthTokenDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;

    public AuthenticateUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
    }

    public async Task<Result<AuthTokenDto>> Handle(AuthenticateUserCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrEmpty(command.Password))
        {
            return AuthErrors.InvalidCredentials;
        }

        var user = await _userRepository.GetByEmailAsync(EmailNormalizer.Normalize(command.Email), cancellationToken);
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            return AuthErrors.InvalidCredentials;
        }

        return user.ToAuthTokenDto(_accessTokenGenerator.Generate(user));
    }
}
