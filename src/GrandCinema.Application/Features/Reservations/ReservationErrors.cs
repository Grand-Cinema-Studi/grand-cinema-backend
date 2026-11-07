using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Features.Reservations;

public static class ReservationErrors
{
    public static readonly Error ScreeningNotFound =
        Error.NotFound("screening-not-found", "The screening does not exist.");

    // the only date rule of a hold is that the screening has not started.
    public static readonly Error ScreeningStarted =
        Error.Conflict("screening-started", "The screening has already started.");

    // Also the answer of the second of two customers who want the last seat at the same time.
    public static readonly Error NotEnoughSeats =
        Error.Conflict("not-enough-seats", "Not enough seats are left for this screening.");

    // Also the answer for the reservation of another customer: its existence is never revealed.
    public static readonly Error ReservationNotFound =
        Error.NotFound("reservation-not-found", "The reservation does not exist.");

    // Not paid, unknown, or of another customer: one answer, nothing revealed
    public static readonly Error TicketNotFound =
        Error.NotFound("ticket-not-found", "This reservation has no ticket.");

    public static readonly Error TicketAlreadyUsed =
        Error.Conflict("ticket-already-used", "This ticket has already been used.");

    public static readonly Error TicketForAnotherScreening =
        Error.Conflict("ticket-for-another-screening", "This ticket is for another screening.");

    // A paid reservation is not cancelled online: refunds follow the terms of sale.
    public static readonly Error ReservationConfirmed =
        Error.Conflict("reservation-confirmed", "A paid reservation cannot be cancelled online.");

    // Also when several holds of the same customer (several tabs) would add up to more than 10 seats.
    public static readonly Error SeatLimitReached =
        Error.Conflict("seat-limit-reached", $"At most {Reservation.MaximumSeatsPerCustomer} seats per customer and per screening, held or paid.");

    // no payment page once the 10 minutes have run out, or after a cancel: the seats must be held again.
    public static readonly Error HoldExpired =
        Error.Conflict("hold-expired", "The hold has expired or was cancelled: hold the seats again.");

    // a reservation is paid once.
    public static readonly Error ReservationPaid =
        Error.Conflict("reservation-paid", "This reservation is already paid.");
}
