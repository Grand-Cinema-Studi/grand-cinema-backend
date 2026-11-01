using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Payments;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations.DTOs;

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

    public Task<Result<PaymentPageDto>> Handle(StartPaymentCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
