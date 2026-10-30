using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Auth.Commands;
using GrandCinema.Application.Features.Auth.DTOs;
using GrandCinema.Application.Features.Auth.Queries;
using GrandCinema.Application.Features.Movies.DTOs;
using GrandCinema.Application.Features.Movies.Queries;
using GrandCinema.Application.Features.Reservations.Commands;
using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Application.Features.Reservations.Queries;
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
        services.AddScoped<ICommandHandler<RegisterUserCommand, Result<AuthTokenDto>>, RegisterUserHandler>();
        services.AddScoped<ICommandHandler<AuthenticateUserCommand, Result<AuthTokenDto>>, AuthenticateUserHandler>();
        services.AddScoped<IQueryHandler<GetCurrentUserQuery, Result<UserDto>>, GetCurrentUserHandler>();
        services.AddScoped<IQueryHandler<GetMoviesShowingQuery, IReadOnlyList<MovieDto>>, GetMoviesShowingHandler>();
        services.AddScoped<ICommandHandler<DeleteCurrentUserCommand, Result<Guid>>, DeleteCurrentUserHandler>();
        services.AddScoped<IQueryHandler<GetMovieDetailsQuery, Result<MovieDetailsDto>>, GetMovieDetailsHandler>();
        services.AddScoped<ICommandHandler<HoldSeatsCommand, Result<ReservationDto>>, HoldSeatsHandler>();
        services.AddScoped<IQueryHandler<GetReservationQuery, Result<ReservationDto>>, GetReservationHandler>();

        return services;
    }
}
