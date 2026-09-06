namespace HcbeApi.Models;

public static class OrganizerStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Suspended = "Suspended";
}

public static class TicketOrderStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
    public const string PartiallyRefunded = "PartiallyRefunded";
    public const string Refunded = "Refunded";
}

public sealed class CommunityOrganizer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public string? DisplayNameEn { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string Status { get; set; } = OrganizerStatuses.Pending;
    public string? ReviewNotes { get; set; }
    public string? StripeAccountId { get; set; }
    public bool StripeDetailsSubmitted { get; set; }
    public bool StripeChargesEnabled { get; set; }
    public bool StripePayoutsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public ICollection<Event> Events { get; set; } = new List<Event>();
}

public sealed class EventTicketTier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public long PriceCents { get; set; }
    public string Currency { get; set; } = "cad";
    public int Quantity { get; set; }
    public int MaxPerOrder { get; set; } = 10;
    public DateTime? SalesStartUtc { get; set; }
    public DateTime? SalesEndUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<EventTicketOrderItem> OrderItems { get; set; } = new List<EventTicketOrderItem>();
}

public sealed class EventPromoCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public int PercentOff { get; set; }
    public long? AmountOffCents { get; set; }
    public int? MaxRedemptions { get; set; }
    public int RedemptionCount { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EventTicketOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = TicketOrderStatuses.Pending;
    public string Currency { get; set; } = "cad";
    public long SubtotalCents { get; set; }
    public long DiscountCents { get; set; }
    public long PlatformFeeCents { get; set; }
    public long TotalCents { get; set; }
    public long RefundedAmountCents { get; set; }
    public Guid? PromoCodeId { get; set; }
    public EventPromoCode? PromoCode { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeAccountId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(30);
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? RefundedAtUtc { get; set; }
    public ICollection<EventTicketOrderItem> Items { get; set; } = new List<EventTicketOrderItem>();
    public ICollection<EventTicket> Tickets { get; set; } = new List<EventTicket>();
}

public sealed class EventTicketOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public EventTicketOrder Order { get; set; } = null!;
    public Guid TierId { get; set; }
    public EventTicketTier Tier { get; set; } = null!;
    public string TierName { get; set; } = string.Empty;
    public string? TierNameEn { get; set; }
    public int Quantity { get; set; }
    public long UnitPriceCents { get; set; }
    public long LineTotalCents { get; set; }
}

public sealed class EventTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public EventTicketOrder Order { get; set; } = null!;
    public Guid TierId { get; set; }
    public EventTicketTier Tier { get; set; } = null!;
    public string TicketCode { get; set; } = string.Empty;
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public string Status { get; set; } = "Valid";
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CheckedInAtUtc { get; set; }
    public DateTime? TransferredAtUtc { get; set; }
}

public sealed class AdvertisingCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SubmittedByUserId { get; set; }
    public User? SubmittedByUser { get; set; }
    public Guid? OrganizerId { get; set; }
    public CommunityOrganizer? Organizer { get; set; }
    public string AdvertiserName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? BodyEn { get; set; }
    public string? ImageUrl { get; set; }
    public string DestinationUrl { get; set; } = string.Empty;
    public string Placements { get; set; } = "Homepage";
    public string? TargetLanguage { get; set; }
    public string? TargetProvince { get; set; }
    public string? TargetZone { get; set; }
    public string Status { get; set; } = "Draft";
    public string? ReviewNotes { get; set; }
    public long BudgetCents { get; set; }
    public string Currency { get; set; } = "cad";
    public long ImpressionCount { get; set; }
    public long ClickCount { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}
