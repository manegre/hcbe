namespace HcbeApi.Services;

public sealed record PaymentCheckoutLine(string Name, long UnitAmountCents, int Quantity);
public sealed record PaymentCheckoutRequest(Guid TransactionId, string Kind, long AmountCents,
    string Currency, string ProductName, string? StripePriceId, string Email, Guid? UserId,
    string? StripeCustomerId, bool IsRecurring, string SuccessUrl, string CancelUrl,
    IReadOnlyList<PaymentCheckoutLine>? Lines = null, IReadOnlyDictionary<string, string>? Metadata = null,
    string? ConnectedAccountId = null, long? ApplicationFeeAmountCents = null,
    DateTime? ExpiresAtUtc = null);
public sealed record PaymentCheckoutResult(string SessionId, string Url, string? CustomerId);
public sealed record PaymentRefundResult(string RefundId, string Status, long AmountCents);
public sealed record VerifiedPaymentEvent(string Id, string Type, string ObjectJson);

public interface IPaymentGateway
{
    bool IsEnabled { get; }
    Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken);
    Task<string> CreateBillingPortalAsync(string customerId, string returnUrl, CancellationToken cancellationToken);
    Task CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken);
    Task<PaymentRefundResult> RefundAsync(string paymentIntentId, long? amountCents, string? reason,
        string idempotencyKey, CancellationToken cancellationToken, string? connectedAccountId = null);
    VerifiedPaymentEvent VerifyWebhook(string payload, string signature);
}
