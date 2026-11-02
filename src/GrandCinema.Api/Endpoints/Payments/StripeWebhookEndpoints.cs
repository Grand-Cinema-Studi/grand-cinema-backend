using GrandCinema.Api.Errors;
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Features.Payments.Commands;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GrandCinema.Api.Endpoints.Payments;

public static class StripeWebhookEndpoints
{
    public static IEndpointRouteBuilder MapStripeWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments/stripe-webhook", ReceiveNotification)
            // Called by Stripe, without any access token: the signature of the notification is checked instead
            .AllowAnonymous()
            .WithName("ReceiveStripeNotification")
            .WithTags("Payments")
            .WithSummary("Receive a payment notification from Stripe")
            .WithDescription("Called by Stripe only (webhook). The signature (Stripe-Signature header) is checked before anything else: 400 when it is missing or wrong. A completed payment confirms its reservation, once; a payment that arrives after its seats were freed, or a second payment of the same reservation, is refunded.")
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<Results<Ok, ProblemHttpResult>> ReceiveNotification(
        HttpRequest request,
        [FromHeader(Name = "Stripe-Signature")] string? signature,
        ICommandHandler<ConfirmPaymentCommand, Result<PaymentOutcome>> handler,
        CancellationToken cancellationToken)
    {
        // The body exactly as received: the signature is computed on it, byte for byte
        using var reader = new StreamReader(request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        var result = await handler.Handle(new ConfirmPaymentCommand(payload, signature ?? string.Empty), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok();
    }
}
