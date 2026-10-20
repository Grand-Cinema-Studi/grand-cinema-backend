using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Auth.DTOs;
using GrandCinema.Application.Features.Auth.DTOs.Mappings;

namespace GrandCinema.Application.Features.Auth.Queries;

public sealed class GetCurrentUserHandler : IQueryHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        return user.ToUserDto();
    }
}
