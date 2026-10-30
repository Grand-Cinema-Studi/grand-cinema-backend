using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;

namespace GrandCinema.Application.Features.Reservations.DTOs.Mappings;

public static class ReservationMappings
{
    public static ReservationDto ToReservationDto(this Reservation reservation, DateTime nowUtc) => new()
    {
        ReservationId = reservation.Id,
        ScreeningId = reservation.ScreeningId,
        // An expired hold is shown as such at once, even before the next hold of the screening marks it "Expired"
        Status = (reservation.HasExpired(nowUtc) ? ReservationStatus.Expired : reservation.Status).ToString(),
        // Rows A to Z, then AA…, as in the room
        Seats = reservation.ReservedSeats
            .Select(reserved => reserved.Seat)
            .OrderBy(seat => seat.Row.Length)
            .ThenBy(seat => seat.Row, StringComparer.Ordinal)
            .ThenBy(seat => seat.Number)
            .Select(seat => $"{seat.Row}{seat.Number}")
            .ToList(),
        TotalAmount = reservation.TotalAmount,
        ExpiresAt = reservation.ExpiresAt
    };
}
