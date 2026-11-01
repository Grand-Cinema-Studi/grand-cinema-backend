using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Payments;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;
using GrandCinema.Domain.Enums;

namespace GrandCinema.Application.Features.Reservations.Commands;

public class StartPaymentHandler : ICommandHandler<StartPaymentCommand, Result<PaymentPageDto>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly TimeProvider _timeProvider;

    public StartPaymentHandler(IReservationRepository reservationRepository, IPaymentGateway paymentGateway, TimeProvider timeProvider)
    {
        _reservationRepository = reservationRepository;
        _paymentGateway = paymentGateway;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PaymentPageDto>> Handle(StartPaymentCommand command, CancellationToken cancellationToken = default)
    {
        // US 3.1, criterion 4: no payment without the terms of sale accepted
        if (!command.AcceptTermsOfSale)
        {
            return Error.Validation(new Dictionary<string, string[]>
            {
                ["acceptTermsOfSale"] = ["The terms of sale must be accepted before paying."]
            });
        }

        var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, cancellationToken);
        // The reservation of another customer answers like an unknown one (OWASP A01)
        if (reservation is null || reservation.UserId != command.UserId)
        {
            return ReservationErrors.ReservationNotFound;
        }

        if (reservation.Status == ReservationStatus.Confirmed)
        {
            return ReservationErrors.ReservationPaid;
        }

        // Expired, about to be released, or cancelled: no payment page for seats that are no longer held
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        if (reservation.Status != ReservationStatus.PendingPayment || reservation.HasExpired(nowUtc))
        {
            return ReservationErrors.HoldExpired;
        }

        await _reservationRepository.RecordTermsOfSaleAsync(reservation.Id, TermsOfSale.CurrentVersion, nowUtc, cancellationToken);
        var checkoutUrl = await _paymentGateway.CreateCheckoutAsync(
            new PaymentRequest(reservation.Id, reservation.TotalAmount, $"Le Grand Cinéma – {reservation.ReservedSeats.Count} place(s)"),
            cancellationToken);

        return new PaymentPageDto { CheckoutUrl = checkoutUrl };
    }
}
