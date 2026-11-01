using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Domain.Entities;

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

    // A paid reservation is not cancelled online: refunds follow the terms of sale (US 3.1).
    public static readonly Error ReservationConfirmed =
        Error.Conflict("reservation-confirmed", "A paid reservation cannot be cancelled online.");

    // US 2.3, criterion 7: also when several holds of the same customer (several tabs) would add up to more than 10 seats.
    public static readonly Error SeatLimitReached =
        Error.Conflict("seat-limit-reached", $"At most {Reservation.MaximumSeatsPerCustomer} seats per customer and per screening, held or paid.");
}
