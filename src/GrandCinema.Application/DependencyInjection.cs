using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Screenings.DTOs;
using GrandCinema.Application.Features.Screenings.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace GrandCinema.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {   
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IQueryHandler<GetScheduleQuery, ScheduleDto>, GetScheduleHandler>();
        
        return services;
    }
}
