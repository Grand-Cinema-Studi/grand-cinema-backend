using System.Net.Mail;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Abstractions.Security;
using GrandCinema.Application.Features.Auth.DTOs;
using GrandCinema.Application.Features.Auth.DTOs.Mappings;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;

namespace GrandCinema.Application.Features.Auth.Commands;

public sealed class RegisterUserHandler : ICommandHandler<RegisterUserCommand, Result<AuthTokenDto>>
{
    // CNIL recommendation for a password-only authentication: at least 12 characters.
    public const int MinimumPasswordLength = 12;
    public const int MaximumPasswordLength = 128;
    private const int MaximumEmailLength = 256;
    private const int MaximumNameLength = 100;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly TimeProvider _timeProvider;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthTokenDto>> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(command);
        if (validationErrors.Count > 0)
        {
            return Error.Validation(validationErrors);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = EmailNormalizer.Normalize(command.Email),
            PasswordHash = _passwordHasher.Hash(command.Password),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            CreatedAt = now,
            // Public sign-up always creates a customer: staff roles are never obtained by registering.
            Role = UserRole.Customer,
            PrivacyPolicyAcceptedAt = now,
            PrivacyPolicyVersion = PrivacyPolicy.CurrentVersion
        };

        if (!await _userRepository.TryAddAsync(user, cancellationToken))
        {
            return AuthErrors.EmailAlreadyUsed;
        }

        return user.ToAuthTokenDto(_accessTokenGenerator.Generate(user));
    }

    private static Dictionary<string, string[]> Validate(RegisterUserCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(command.Email)
            || command.Email.Trim().Length > MaximumEmailLength
            || !MailAddress.TryCreate(command.Email.Trim(), out _))
        {
            errors["email"] = ["The email address is not valid."];
        }

        if (command.Password is null
            || command.Password.Length < MinimumPasswordLength
            || command.Password.Length > MaximumPasswordLength)
        {
            errors["password"] = [$"The password must contain between {MinimumPasswordLength} and {MaximumPasswordLength} characters."];
        }

        if (string.IsNullOrWhiteSpace(command.FirstName) || command.FirstName.Trim().Length > MaximumNameLength)
        {
            errors["firstName"] = [$"The first name is required ({MaximumNameLength} characters at most)."];
        }

        if (string.IsNullOrWhiteSpace(command.LastName) || command.LastName.Trim().Length > MaximumNameLength)
        {
            errors["lastName"] = [$"The last name is required ({MaximumNameLength} characters at most)."];
        }

        // GDPR: the privacy policy is reachable through a link next to the box; no account until the box is ticked.
        if (!command.AcceptPrivacyPolicy)
        {
            errors["acceptPrivacyPolicy"] = ["The privacy policy must be accepted."];
        }

        return errors;
    }
}
