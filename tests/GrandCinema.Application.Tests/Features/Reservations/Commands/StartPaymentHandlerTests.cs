using GrandCinema.Application.Abstractions.Payments;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Reservations;
using GrandCinema.Application.Features.Reservations.Commands;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Reservations.Commands;

public class StartPaymentHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 7, 18, 16, 0, 0, DateTimeKind.Utc);
    private const string CheckoutUrl = "https://checkout.stripe.com/c/pay/cs_test";
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(NowUtc));
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IPaymentGateway> _payments = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Reservation _hold;

    public StartPaymentHandlerTests()
    {
        // 2 seats at 9.50 €: 19.00 €, held at 16:00 UTC until 16:10
        var screening = new Screening { Id = Guid.NewGuid(), MovieId = Guid.NewGuid(), StartsAt = new DateTime(2026, 7, 18, 20, 30, 0, DateTimeKind.Unspecified), Price = 9.5m };
        var seats = new[] { new Seat { Id = Guid.NewGuid(), Row = "C", Number = 4 }, new Seat { Id = Guid.NewGuid(), Row = "C", Number = 5 } };
        _hold = Reservation.Hold(_owner, screening, seats, NowUtc);
        _reservations
            .Setup(r => r.GetByIdAsync(_hold.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_hold);
        _payments
            .Setup(p => p.CreateCheckoutAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CheckoutUrl);
    }

    private StartPaymentHandler CreateHandler() => new(_reservations.Object, _payments.Object, _time);

    private void NoPaymentPageWasCreated() =>
        _payments.Verify(p => p.CreateCheckoutAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_the_terms_of_sale_are_not_accepted_Then_the_request_is_refused_before_any_payment()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new StartPaymentCommand(_owner, _hold.Id, AcceptTermsOfSale: false));

        // Assert: criterion 4
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull().Keys.ShouldContain("acceptTermsOfSale");
        NoPaymentPageWasCreated();
    }

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_another_customer_pays_the_hold_Then_reservation_not_found()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new StartPaymentCommand(Guid.NewGuid(), _hold.Id, AcceptTermsOfSale: true));

        // Assert: same answer as an unknown reservation (OWASP A01)
        result.Error.ShouldBe(ReservationErrors.ReservationNotFound);
        NoPaymentPageWasCreated();
    }

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_the_reservation_is_already_paid_Then_reservation_paid()
    {
        // Arrange
        _hold.Status = ReservationStatus.Confirmed;
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new StartPaymentCommand(_owner, _hold.Id, AcceptTermsOfSale: true));

        // Assert
        result.Error.ShouldBe(ReservationErrors.ReservationPaid);
        NoPaymentPageWasCreated();
    }

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_the_10_minutes_have_run_out_Then_hold_expired()
    {
        // Arrange: 16:10 UTC, the very instant the seats are freed
        _time.Advance(Reservation.HoldDuration);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new StartPaymentCommand(_owner, _hold.Id, AcceptTermsOfSale: true));

        // Assert: no payment page for seats that are no longer held
        result.Error.ShouldBe(ReservationErrors.HoldExpired);
        NoPaymentPageWasCreated();
    }

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_the_hold_is_valid_Then_the_terms_are_recorded_and_the_payment_page_is_created_for_its_amount()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new StartPaymentCommand(_owner, _hold.Id, AcceptTermsOfSale: true));

        // Assert: the version accepted is kept, and Stripe asks for the amount fixed when the seats were held
        result.Value.CheckoutUrl.ShouldBe(CheckoutUrl);
        _reservations.Verify(r => r.RecordTermsOfSaleAsync(_hold.Id, TermsOfSale.CurrentVersion, NowUtc, It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.CreateCheckoutAsync(
            It.Is<PaymentRequest>(request => request.ReservationId == _hold.Id && request.Amount == 19m),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
