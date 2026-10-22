using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Security;
using GrandCinema.Infrastructure.Persistence;
using GrandCinema.Infrastructure.Persistence.Repositories;
using GrandCinema.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GrandCinema.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

        services.AddDbContext<GrandCinemaDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IScreeningReadRepository, ScreeningReadRepository>();
        services.AddScoped<IMovieReadRepository, MovieReadRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Read on first use, not at startup: the EF Core design-time tools build the host without any JWT settings.
        services.AddSingleton(_ => JwtOptions.FromConfiguration(configuration));
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();

        return services;
    }
}
