namespace HcbeApi.Models;

public static class FinanceKinds
{
    public const string Membership = "Membership";
    public const string Donation = "Donation";
}

public static class FinanceStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";
    public const string PartiallyRefunded = "PartiallyRefunded";
    public const string Disputed = "Disputed";
}

public static class MembershipStatuses
{
    public const string Inactive = "Inactive";
    public const string Active = "Active";
    public const string GracePeriod = "GracePeriod";
    public const string Expired = "Expired";
}

public sealed class MembershipPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public long AmountCents { get; set; }
    public string Currency { get; set; } = "cad";
    public string BillingMode { get; set; } = "Annual";
    public string? StripePriceId { get; set; }
    public string BenefitsJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class MembershipStanding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? PlanId { get; set; }
    public MembershipPlan? Plan { get; set; }
    public string Status { get; set; } = MembershipStatuses.Inactive;
    public DateTime? CurrentPeriodStartUtc { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public DateTime? GraceEndsAtUtc { get; set; }
    public bool AutoRenew { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public Guid? LastTransactionId { get; set; }
    public string? LastReminderKey { get; set; }
    public DateTime? LastReminderAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class DonationCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public long GoalAmountCents { get; set; }
    public string Currency { get; set; } = "cad";
    public string? ImageUrl { get; set; }
    public bool AllowRecurring { get; set; } = true;
    public bool IsPublished { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class FinancialTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? MembershipPlanId { get; set; }
    public MembershipPlan? MembershipPlan { get; set; }
    public Guid? DonationCampaignId { get; set; }
    public DonationCampaign? DonationCampaign { get; set; }
    public string Kind { get; set; } = FinanceKinds.Donation;
    public string Status { get; set; } = FinanceStatuses.Pending;
    public long AmountCents { get; set; }
    public long RefundedAmountCents { get; set; }
    public string Currency { get; set; } = "cad";
    public string PayerEmail { get; set; } = string.Empty;
    public string? PayerName { get; set; }
    public bool IsAnonymous { get; set; }
    public bool AllowPublicRecognition { get; set; }
    public string? DonorMessage { get; set; }
    public bool IsRecurring { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeInvoiceId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ReceiptToken { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? RefundedAtUtc { get; set; }
}

public sealed class PaymentWebhookEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProviderEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing";
    public string? Error { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
}
