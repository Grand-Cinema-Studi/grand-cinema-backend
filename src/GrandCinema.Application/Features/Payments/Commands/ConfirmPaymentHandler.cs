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

    public Task<Result<PaymentOutcome>> Handle(ConfirmPaymentCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
