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

    public Task<Result<Guid>> Handle(DeleteCurrentUserCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
