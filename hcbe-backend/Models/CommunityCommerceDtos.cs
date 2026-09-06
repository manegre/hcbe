using System.ComponentModel.DataAnnotations;

namespace HcbeApi.Models;

public sealed record TicketTierDto(Guid Id, Guid EventId, string Name, string? NameEn, string? Description,
    string? DescriptionEn, long PriceCents, string Currency, int Quantity, int SoldQuantity,
    int ReservedQuantity, int AvailableQuantity, int MaxPerOrder, DateTime? SalesStartUtc,
    DateTime? SalesEndUtc, bool IsActive, int DisplayOrder);

public sealed record UpsertTicketTierRequest([Required, MaxLength(100)] string Name, [MaxLength(100)] string? NameEn,
    [MaxLength(500)] string? Description, [MaxLength(500)] string? DescriptionEn,
    [Range(0, 100_000_000)] long PriceCents, [Required, MaxLength(3)] string Currency,
    [Range(1, 100_000)] int Quantity, [Range(1, 50)] int MaxPerOrder,
    DateTime? SalesStartUtc, DateTime? SalesEndUtc, bool IsActive, int DisplayOrder);

public sealed record PromoCodeDto(Guid Id, Guid EventId, string Code, int PercentOff, long? AmountOffCents,
    int? MaxRedemptions, int RedemptionCount, DateTime? StartsAtUtc, DateTime? EndsAtUtc, bool IsActive);

public sealed record UpsertPromoCodeRequest([Required, MaxLength(32)] string Code, [Range(0, 100)] int PercentOff,
    [Range(0, 100_000_000)] long? AmountOffCents, [Range(1, 100_000)] int? MaxRedemptions,
    DateTime? StartsAtUtc, DateTime? EndsAtUtc, bool IsActive);

public sealed record TicketSelectionRequest(Guid TierId, [Range(1, 50)] int Quantity);
public sealed record CreateTicketCheckoutRequest([Required, MaxLength(160)] string BuyerName,
    [Required, EmailAddress, MaxLength(320)] string BuyerEmail,
    [Required, MinLength(1)] IReadOnlyList<TicketSelectionRequest> Items,
    [MaxLength(32)] string? PromoCode = null);

public sealed record TicketOrderItemDto(Guid Id, Guid TierId, string TierName, string? TierNameEn,
    int Quantity, long UnitPriceCents, long LineTotalCents);
public sealed record TicketDto(Guid Id, string TicketCode, Guid TierId, string TierName, string? TierNameEn,
    string AttendeeName, string AttendeeEmail, string Status, DateTime IssuedAtUtc,
    DateTime? CheckedInAtUtc, DateTime? TransferredAtUtc);
public sealed record TicketOrderDto(Guid Id, Guid EventId, string EventTitle, string? EventTitleEn,
    string BuyerName, string BuyerEmail, string Status, string Currency, long SubtotalCents,
    long DiscountCents, long PlatformFeeCents, long TotalCents, long RefundedAmountCents,
    string OrderNumber, string? CheckoutUrl, string? TicketPdfUrl, DateTime CreatedAtUtc,
    DateTime? PaidAtUtc, IReadOnlyList<TicketOrderItemDto> Items, IReadOnlyList<TicketDto> Tickets);
public sealed record TicketCheckoutDto(Guid OrderId, string Status, string? CheckoutUrl,
    string SessionId, string OrderNumber, string AccessToken, string? TicketPdfUrl);
public sealed record TransferTicketRequest([Required, MaxLength(160)] string AttendeeName,
    [Required, EmailAddress, MaxLength(320)] string AttendeeEmail);
public sealed record RefundTicketOrderRequest([Range(1, 100_000_000)] long? AmountCents, [MaxLength(500)] string? Reason);

public sealed record TicketingDashboardDto(int Orders, int TicketsSold, int CheckedIn,
    long GrossRevenueCents, long RefundedAmountCents, string Currency,
    IReadOnlyList<TicketOrderDto> RecentOrders);

public sealed record CommunityOrganizerDto(Guid Id, Guid UserId, string DisplayName, string? DisplayNameEn,
    string ContactEmail, string? ContactPhone, string? WebsiteUrl, string? Description,
    string? DescriptionEn, string Status, string? ReviewNotes, bool HasStripeAccount,
    bool StripeDetailsSubmitted, bool StripeChargesEnabled, bool StripePayoutsEnabled,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? ReviewedAtUtc);
public sealed record UpsertOrganizerRequest([Required, MaxLength(160)] string DisplayName,
    [MaxLength(160)] string? DisplayNameEn, [Required, EmailAddress, MaxLength(320)] string ContactEmail,
    [MaxLength(40)] string? ContactPhone, [MaxLength(500)] string? WebsiteUrl,
    [MaxLength(3000)] string? Description, [MaxLength(3000)] string? DescriptionEn);
public sealed record ReviewOrganizerRequest([Required, MaxLength(30)] string Status, [MaxLength(2000)] string? ReviewNotes);
public sealed record OrganizerOnboardingDto(string Url, bool AlreadyComplete);
public sealed record OrganizerEventDto(Guid Id, string Title, string? TitleEn, DateTime Date,
    string? Location, string Format, string Status, long PriceCents, string Currency,
    int TicketQuantity, int TicketsSold, DateTime CreatedAtUtc);
public sealed record UpsertOrganizerEventRequest([Required, MaxLength(180)] string Title,
    [MaxLength(180)] string? TitleEn, [Required, MaxLength(5000)] string Description,
    [MaxLength(5000)] string? DescriptionEn, DateTime Date, DateTime? EndDate,
    [MaxLength(200)] string? Location, [MaxLength(200)] string? LocationEn,
    [Required, MaxLength(20)] string Format, [MaxLength(1000)] string? ImageUrl,
    [Range(0, 100_000_000)] long PriceCents, [Required, MaxLength(3)] string Currency,
    [Range(1, 100_000)] int TicketQuantity);

public sealed record AdvertisingCampaignDto(Guid Id, Guid? OrganizerId, string AdvertiserName,
    string ContactEmail, string Title, string? TitleEn, string Body, string? BodyEn,
    string? ImageUrl, string DestinationUrl, IReadOnlyList<string> Placements,
    string? TargetLanguage, string? TargetProvince, string? TargetZone, string Status,
    string? ReviewNotes, long BudgetCents, string Currency, long ImpressionCount,
    long ClickCount, DateTime StartsAtUtc, DateTime EndsAtUtc, DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc, DateTime? ReviewedAtUtc);
public sealed record UpsertAdvertisingCampaignRequest([Required, MaxLength(160)] string AdvertiserName,
    [Required, EmailAddress, MaxLength(320)] string ContactEmail, [Required, MaxLength(180)] string Title,
    [MaxLength(180)] string? TitleEn, [Required, MaxLength(1500)] string Body,
    [MaxLength(1500)] string? BodyEn, [MaxLength(1000)] string? ImageUrl,
    [Required, MaxLength(1000)] string DestinationUrl, [Required, MinLength(1)] IReadOnlyList<string> Placements,
    [MaxLength(10)] string? TargetLanguage, [MaxLength(100)] string? TargetProvince,
    [MaxLength(100)] string? TargetZone, [Range(0, 100_000_000)] long BudgetCents,
    [Required, MaxLength(3)] string Currency, DateTime StartsAtUtc, DateTime EndsAtUtc);
public sealed record ReviewAdvertisingCampaignRequest([Required, MaxLength(30)] string Status,
    [MaxLength(2000)] string? ReviewNotes);
