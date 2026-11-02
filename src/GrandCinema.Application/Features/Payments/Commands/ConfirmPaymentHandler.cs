using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Payments;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Payments.Commands;

public class ConfirmPaymentHandler : ICommandHandler<ConfirmPaymentCommand, Result<PaymentOutcome>>
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IReservationRepository _reservationRepository;
    private readonly TimeProvider _timeProvider;

    public ConfirmPaymentHandler(IPaymentGateway paymentGateway, IReservationRepository reservationRepository, TimeProvider timeProvider)
    {
        _paymentGateway = paymentGateway;
        _reservationRepository = reservationRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PaymentOutcome>> Handle(ConfirmPaymentCommand command, CancellationToken cancellationToken = default)
    {
        // The signature first: an unsigned notification says nothing that can be trusted
        var notification = _paymentGateway.ReadNotification(command.Payload, command.Signature);
        if (!notification.IsGenuine)
        {
            return PaymentErrors.NotificationNotGenuine;
        }

        // Stripe also sends other events (payment page opened, expired…): only a completed payment matters
        if (notification.PaidReservationId is not { } reservationId || notification.PaymentReference is not { } paymentReference)
        {
            return PaymentOutcome.Ignored;
        }

        var confirmation = await _reservationRepository.ConfirmPaymentAsync(
            reservationId, paymentReference, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        if (confirmation == PaymentConfirmation.Confirmed)
        {
            return PaymentOutcome.Confirmed;
        }

        if (confirmation == PaymentConfirmation.AlreadyConfirmed)
        {
            return PaymentOutcome.AlreadyConfirmed;
        }

        // Paid after its seats were freed, or paid twice: no seat is sold for this money, it goes back
        await _paymentGateway.RefundAsync(paymentReference, cancellationToken);
        return PaymentOutcome.Refunded;
    }
}
