using GrandCinema.Application.Abstractions.Security;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Features.Auth.DTOs.Mappings;

internal static class UserMappings
{
    public static UserDto ToUserDto(this User user) => new()
    {
        UserId = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role.ToString()
    };

    public static AuthTokenDto ToAuthTokenDto(this User user, AccessToken accessToken) => new()
    {
        AccessToken = accessToken.Value,
        ExpiresAt = accessToken.ExpiresAt,
        User = user.ToUserDto()
    };
}
