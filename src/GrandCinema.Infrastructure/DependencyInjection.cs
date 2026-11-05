using GrandCinema.Application.Abstractions.Payments;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Security;
using GrandCinema.Application.Features.Reservations.Queries;
using GrandCinema.Infrastructure.Payments;
using GrandCinema.Infrastructure.Persistence;
using GrandCinema.Infrastructure.Persistence.Queries;
using GrandCinema.Infrastructure.Persistence.Repositories;
using GrandCinema.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;

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
        services.AddScoped<IReservationRepository, ReservationRepository>();

        // Reads of aggregates, projected by PostgreSQL (ADR 0004)
        services.AddScoped<IReservationTrackingQueries, ReservationTrackingQueries>();

        // Read on first use, not at startup: the EF Core design-time tools build the host without any JWT settings.
        services.AddSingleton(_ => JwtOptions.FromConfiguration(configuration));
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();

        // Read on first use, as JwtOptions: the API tests replace Stripe and never need its keys
        services.AddSingleton(_ => StripeOptions.FromConfiguration(configuration));
        // One client for the whole application, built from the test key on first use
        services.AddSingleton<IStripeClient>(provider => new StripeClient(provider.GetRequiredService<StripeOptions>().SecretKey));
        services.AddSingleton<IPaymentGateway, StripePaymentGateway>();

        services.AddScoped<IProgrammingRepository, ProgrammingRepository>();

        return services;
    }
}
