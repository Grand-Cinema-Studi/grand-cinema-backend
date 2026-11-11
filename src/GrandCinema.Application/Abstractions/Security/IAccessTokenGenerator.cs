using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Abstractions.Security;

public interface IAccessTokenGenerator
{
    AccessToken Generate(User user);
}
