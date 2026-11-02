using GrandCinema.Application.Abstractions.Payments;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Payments;
using GrandCinema.Application.Features.Payments.Commands;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Payments.Commands;

public class ConfirmPaymentHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 7, 18, 16, 4, 0, DateTimeKind.Utc);
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(NowUtc));
    private readonly Mock<IPaymentGateway> _payments = new();
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Guid _reservationId = Guid.NewGuid();
    private readonly ConfirmPaymentCommand _notification = new("""{"type":"checkout.session.completed"}""", "t=1,v1=signature");

    private ConfirmPaymentHandler CreateHandler() => new(_payments.Object, _reservations.Object, _time);

    // What the gateway reads in the notification, once its signature is checked
    private void StripeSends(PaymentNotification notification) =>
        _payments.Setup(p => p.ReadNotification(_notification.Payload, _notification.Signature)).Returns(notification);

    private void TheDatabaseAnswers(PaymentConfirmation confirmation) =>
        _reservations
            .Setup(r => r.ConfirmPaymentAsync(_reservationId, "pi_1", NowUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmation);

    private void NothingWasConfirmed() =>
        _reservations.Verify(r => r.ConfirmPaymentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_the_notification_is_not_signed_by_stripe_Then_it_is_refused_and_nothing_is_confirmed()
    {
        // Arrange
        StripeSends(PaymentNotification.NotGenuine);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(_notification);

        // Assert
        result.Error.ShouldBe(PaymentErrors.NotificationNotGenuine);
        NothingWasConfirmed();
    }

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_a_payment_is_completed_Then_its_reservation_is_confirmed_at_the_current_instant()
    {
        // Arrange
        StripeSends(PaymentNotification.Paid(_reservationId, "pi_1"));
        TheDatabaseAnswers(PaymentConfirmation.Confirmed);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(_notification);

        // Assert
        result.Value.ShouldBe(PaymentOutcome.Confirmed);
        _payments.Verify(p => p.RefundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_the_payment_arrives_after_the_seats_were_freed_Then_it_is_refunded()
    {
        // Arrange
        StripeSends(PaymentNotification.Paid(_reservationId, "pi_1"));
        TheDatabaseAnswers(PaymentConfirmation.Rejected);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(_notification);

        // Assert: no seat sold, so no money kept
        result.Value.ShouldBe(PaymentOutcome.Refunded);
        _payments.Verify(p => p.RefundAsync("pi_1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("US", "3.1")]
    public async Task When_stripe_sends_another_event_Then_it_is_ignored()
    {
        // Arrange
        StripeSends(PaymentNotification.Ignored);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(_notification);

        // Assert
        result.Value.ShouldBe(PaymentOutcome.Ignored);
        NothingWasConfirmed();
    }
}
