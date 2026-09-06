using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace HcbeApi.Services;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly FinanceOptions options;
    private readonly StripeClient? client;

    public StripePaymentGateway(IOptions<FinanceOptions> configured)
    {
        options = configured.Value;
        if (options.Enabled && !string.IsNullOrWhiteSpace(options.SecretKey))
            client = new StripeClient(options.SecretKey);
    }

    public bool IsEnabled => options.Enabled && client != null && !string.IsNullOrWhiteSpace(options.WebhookSecret);

    public async Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var metadata = new Dictionary<string, string>
        {
            ["hcbe_transaction_id"] = request.TransactionId.ToString("N"),
            ["hcbe_kind"] = request.Kind
        };
        if (request.Metadata != null)
            foreach (var pair in request.Metadata) metadata[pair.Key] = pair.Value;
        var sessionOptions = new SessionCreateOptions
        {
            Mode = request.IsRecurring ? "subscription" : "payment",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            ClientReferenceId = request.UserId?.ToString("N"),
            Customer = request.StripeCustomerId,
            CustomerEmail = string.IsNullOrWhiteSpace(request.StripeCustomerId) ? request.Email : null,
            AutomaticTax = new SessionAutomaticTaxOptions { Enabled = options.AutomaticTaxEnabled },
            ExpiresAt = request.ExpiresAtUtc,
            Metadata = metadata,
            LineItems = request.Lines?.Count > 0
                ? request.Lines.Select(line => new SessionLineItemOptions
                {
                    Quantity = line.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency,
                        UnitAmount = line.UnitAmountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = line.Name }
                    }
                }).ToList()
                : [new SessionLineItemOptions
                {
                    Quantity = 1,
                    Price = string.IsNullOrWhiteSpace(request.StripePriceId) ? null : request.StripePriceId,
                    PriceData = string.IsNullOrWhiteSpace(request.StripePriceId)
                        ? new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency,
                            UnitAmount = request.AmountCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions { Name = request.ProductName },
                            Recurring = request.IsRecurring
                                ? new SessionLineItemPriceDataRecurringOptions { Interval = "year" }
                                : null
                        }
                        : null
                }]
        };
        if (request.ApplicationFeeAmountCents > 0)
            sessionOptions.PaymentIntentData = new SessionPaymentIntentDataOptions { ApplicationFeeAmount = request.ApplicationFeeAmountCents };
        if (request.IsRecurring)
        {
            sessionOptions.SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = metadata
            };
        }

        var session = await new SessionService(client).CreateAsync(
            sessionOptions,
            new RequestOptions { IdempotencyKey = $"hcbe-checkout-{request.TransactionId:N}", StripeAccount = request.ConnectedAccountId },
            cancellationToken);
        return new PaymentCheckoutResult(session.Id, session.Url, session.CustomerId);
    }

    public async Task<string> CreateBillingPortalAsync(string customerId, string returnUrl, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var session = await new Stripe.BillingPortal.SessionService(client).CreateAsync(
            new Stripe.BillingPortal.SessionCreateOptions { Customer = customerId, ReturnUrl = returnUrl },
            cancellationToken: cancellationToken);
        return session.Url;
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        await new SubscriptionService(client).CancelAsync(subscriptionId, cancellationToken: cancellationToken);
    }

    public async Task<PaymentRefundResult> RefundAsync(string paymentIntentId, long? amountCents, string? reason,
        string idempotencyKey, CancellationToken cancellationToken, string? connectedAccountId = null)
    {
        EnsureEnabled();
        var refund = await new RefundService(client).CreateAsync(new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Amount = amountCents,
            Metadata = string.IsNullOrWhiteSpace(reason) ? null : new Dictionary<string, string> { ["hcbe_reason"] = reason.Trim() }
        }, new RequestOptions { IdempotencyKey = idempotencyKey, StripeAccount = connectedAccountId }, cancellationToken);
        return new PaymentRefundResult(refund.Id, refund.Status ?? "pending", refund.Amount);
    }

    public VerifiedPaymentEvent VerifyWebhook(string payload, string signature)
    {
        if (!IsEnabled) throw new InvalidOperationException("Payments are not configured.");
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, options.WebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException) when (!string.IsNullOrWhiteSpace(options.ConnectWebhookSecret))
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, options.ConnectWebhookSecret, throwOnApiVersionMismatch: false);
        }
        using var document = JsonDocument.Parse(payload);
        var objectJson = document.RootElement.GetProperty("data").GetProperty("object").GetRawText();
        return new VerifiedPaymentEvent(stripeEvent.Id, stripeEvent.Type, objectJson);
    }

    private void EnsureEnabled()
    {
        if (!IsEnabled) throw new InvalidOperationException("Payments are not configured.");
    }
}
