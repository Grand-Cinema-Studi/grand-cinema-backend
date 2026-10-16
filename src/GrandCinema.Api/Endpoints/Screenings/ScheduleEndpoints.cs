using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Screenings.DTOs;
using GrandCinema.Application.Features.Screenings.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Screenings;

public static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/schedule", GetSchedule)
            .AllowAnonymous()
            .WithName("GetSchedule")
            .WithTags("Schedule")
            .WithSummary("Movies and screenings of a day")
            .WithDescription("Returns the movies showing on the given date (today if omitted), with their screenings sorted by time.");

        return app;
    }

    private static async Task<Ok<ScheduleDto>> GetSchedule(
        DateOnly? date,
        IQueryHandler<GetScheduleQuery, ScheduleDto> handler,
        CancellationToken cancellationToken)
    {
        var schedule = await handler.Handle(new GetScheduleQuery(date), cancellationToken);
        return TypedResults.Ok(schedule);
    }
}
