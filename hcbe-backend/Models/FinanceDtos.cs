namespace HcbeApi.Models;

public sealed record MembershipPlanDto(Guid Id, string Name, string? NameEn, string Description,
    string? DescriptionEn, long AmountCents, string Currency, string BillingMode,
    string? StripePriceId, IReadOnlyList<string> Benefits, bool IsActive, int DisplayOrder);

public sealed record UpsertMembershipPlanRequest(string Name, string? NameEn, string Description,
    string? DescriptionEn, long AmountCents, string Currency, string BillingMode,
    string? StripePriceId, IReadOnlyList<string>? Benefits, bool IsActive, int DisplayOrder);

public sealed record MembershipStandingDto(string Status, DateTime? CurrentPeriodStartUtc,
    DateTime? CurrentPeriodEndUtc, DateTime? GraceEndsAtUtc, bool AutoRenew,
    bool HasBillingAccount, bool HasActiveSubscription, MembershipPlanDto? Plan,
    string? VerificationCode, string? VerificationUrl);

public sealed record DonationCampaignDto(Guid Id, string Slug, string Title, string? TitleEn,
    string Description, string? DescriptionEn, long GoalAmountCents, long RaisedAmountCents,
    string Currency, string? ImageUrl, bool AllowRecurring, bool IsPublished,
    DateTime? StartsAtUtc, DateTime? EndsAtUtc, int SupporterCount);

public sealed record UpsertDonationCampaignRequest(string Slug, string Title, string? TitleEn,
    string Description, string? DescriptionEn, long GoalAmountCents, string Currency,
    string? ImageUrl, bool AllowRecurring, bool IsPublished, DateTime? StartsAtUtc, DateTime? EndsAtUtc);

public sealed record FinancialTransactionDto(Guid Id, string Kind, string Status, long AmountCents,
    long RefundedAmountCents, string Currency, string PayerEmail, string? PayerName,
    bool IsAnonymous, bool AllowPublicRecognition, bool IsRecurring, string ReceiptNumber,
    string? ReceiptUrl, Guid? MembershipPlanId, Guid? DonationCampaignId,
    string? CampaignTitle, DateTime CreatedAtUtc, DateTime? PaidAtUtc, DateTime? RefundedAtUtc);

public sealed record MemberFinanceSummaryDto(MembershipStandingDto Membership,
    IReadOnlyList<MembershipPlanDto> Plans, IReadOnlyList<FinancialTransactionDto> Transactions);

public sealed record CreateMembershipCheckoutRequest(Guid PlanId);
public sealed record CreateDonationCheckoutRequest(Guid? CampaignId, long AmountCents, string Currency,
    string Email, string? Name, bool IsAnonymous, bool AllowPublicRecognition,
    string? Message, bool IsRecurring);
public sealed record CheckoutSessionDto(Guid TransactionId, string CheckoutUrl, string SessionId);
public sealed record CheckoutResultDto(string Status, string Kind, long AmountCents, string Currency,
    string? ReceiptUrl, string? ReturnUrl);
public sealed record BillingPortalDto(string Url);
public sealed record RefundTransactionRequest(long? AmountCents, string? Reason);
public sealed record UpdateMembershipStandingRequest(string Status, DateTime? CurrentPeriodEndUtc, string? Note);
public sealed record FinanceDashboardDto(long PaidAmountCents, long RefundedAmountCents,
    long MembershipRevenueCents, long DonationRevenueCents, int ActiveMembers, int ExpiringMembers,
    int PaidTransactionCount, IReadOnlyList<FinancialTransactionDto> RecentTransactions);
public sealed record AdminMembershipDto(Guid UserId, string MemberName, string Email, string Status,
    string? PlanName, DateTime? CurrentPeriodEndUtc, DateTime? GraceEndsAtUtc, bool AutoRenew);
public sealed record MembershipVerificationDto(bool IsValid, string Status, string MemberName,
    string? PlanName, DateTime? ValidUntilUtc, string VerificationCode);
