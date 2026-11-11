using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Abstractions.Persistence;

/// <summary>The outcome of a hold: the hold, or the reason why it was refused.</summary>
public sealed record class HoldResult(Reservation? Hold, HoldRefusal? Refusal)
{
    public static HoldResult Held(Reservation hold) => new(hold, null);

    public static HoldResult Refused(HoldRefusal refusal) => new(null, refusal);
}
