using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Reservations;

public static class ReservationErrors
{
    public static readonly Error ScreeningNotFound =
        Error.NotFound("screening-not-found", "The screening does not exist.");

    // Decision of 29/10/2026: the only date rule of a hold is that the screening has not started.
    public static readonly Error ScreeningStarted =
        Error.Conflict("screening-started", "The screening has already started.");

    // Also the answer of the second of two customers who want the last seat at the same time (criterion 4).
    public static readonly Error NotEnoughSeats =
        Error.Conflict("not-enough-seats", "Not enough seats are left for this screening.");

    // Also the answer for the reservation of another customer: its existence is never revealed.
    public static readonly Error ReservationNotFound =
        Error.NotFound("reservation-not-found", "The reservation does not exist.");
}
