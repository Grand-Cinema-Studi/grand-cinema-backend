using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Programming;

public static class ScreeningRules
{
    // A screening is scheduled in the future (local time of the cinema), at a positive price; null when both are valid
    public static Error? Validate(DateTime startsAt, decimal price, DateTime nowLocal)
    {
        var errors = new Dictionary<string, string[]>();
        if (startsAt.Kind != DateTimeKind.Unspecified)
        {
            errors["startsAt"] = ["A local time of the cinema, without time zone, is expected: 2026-11-30T20:30:00."];
        }
        else if (startsAt <= nowLocal)
        {
            errors["startsAt"] = ["A screening is scheduled in the future."];
        }
        if (price <= 0)
        {
            errors["price"] = ["The price must be greater than zero."];
        }

        return errors.Count == 0 ? null : Error.Validation(errors);
    }

    // The screenings of the room that could overlap: a movie never lasts a whole day
    public static (DateTime From, DateTime To) NeighbourhoodOf(DateTime startsAt) => (startsAt.AddDays(-1), startsAt.AddDays(1));
}
