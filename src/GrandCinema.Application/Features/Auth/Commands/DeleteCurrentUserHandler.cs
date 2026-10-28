using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Auth.Commands;

public class DeleteCurrentUserHandler : ICommandHandler<DeleteCurrentUserCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;

    public DeleteCurrentUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<Guid>> Handle(DeleteCurrentUserCommand command, CancellationToken cancellationToken = default)
    {
        // GDPR right to erasure: the account and its personal data are deleted; its reservations stay, detached from it.
        if (!await _userRepository.DeleteAsync(command.UserId, cancellationToken))
        {
            return AuthErrors.UserNotFound;
        }

        return command.UserId;
    }
}
