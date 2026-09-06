namespace HcbeApi.Models;

public static class CommunityProgramStatuses
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Active = "Active";
    public const string Cancelled = "Cancelled";
    public const string Withdrawn = "Withdrawn";
    public const string Published = "Published";
}

public sealed class CommunityBusiness
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? Services { get; set; }
    public string? ServicesEn { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? ServiceRegions { get; set; }
    public string Status { get; set; } = CommunityProgramStatuses.Submitted;
    public bool IsFeatured { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}

public sealed class NewcomerJourney
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateOnly? ArrivalDate { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string PreferredLanguage { get; set; } = "fr";
    public string NeedsJson { get; set; } = "[]";
    public string CompletedStepsJson { get; set; } = "[]";
    public bool MentorRequested { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class FamilyHousehold
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
    public string HouseholdName { get; set; } = string.Empty;
    public string Status { get; set; } = CommunityProgramStatuses.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<FamilyHouseholdMember> Members { get; set; } = new List<FamilyHouseholdMember>();
}

public sealed class FamilyHouseholdMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HouseholdId { get; set; }
    public FamilyHousehold? Household { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string Status { get; set; } = CommunityProgramStatuses.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AppointmentOffering
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Mode { get; set; } = "Online";
    public string? Location { get; set; }
    public string? LocationEn { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<AppointmentSlot> Slots { get; set; } = new List<AppointmentSlot>();
}

public sealed class AppointmentSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OfferingId { get; set; }
    public AppointmentOffering? Offering { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public int Capacity { get; set; } = 1;
    public bool IsCancelled { get; set; }
    public ICollection<AppointmentBooking> Bookings { get; set; } = new List<AppointmentBooking>();
}

public sealed class AppointmentBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SlotId { get; set; }
    public AppointmentSlot? Slot { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = CommunityProgramStatuses.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAtUtc { get; set; }
}

public sealed class PartnerBenefit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public Partner? Partner { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? Terms { get; set; }
    public string? TermsEn { get; set; }
    public string? RedemptionInstructions { get; set; }
    public string? RedemptionInstructionsEn { get; set; }
    public string? SharedCode { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public int? MaxClaims { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<PartnerBenefitClaim> Claims { get; set; } = new List<PartnerBenefitClaim>();
}

public sealed class PartnerBenefitClaim
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BenefitId { get; set; }
    public PartnerBenefit? Benefit { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string RedemptionCode { get; set; } = string.Empty;
    public string Status { get; set; } = CommunityProgramStatuses.Active;
    public DateTime ClaimedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RedeemedAtUtc { get; set; }
}

public sealed class GrantApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GrantProgramId { get; set; }
    public GrantProgram? GrantProgram { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public string AnswersJson { get; set; } = "{}";
    public string DocumentsJson { get; set; } = "[]";
    public string Status { get; set; } = CommunityProgramStatuses.Submitted;
    public string? AdminNotes { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}

public sealed class SponsorshipPackage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string DeliverablesJson { get; set; } = "[]";
    public long AmountCents { get; set; }
    public string Currency { get; set; } = "cad";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SponsorshipRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? PackageId { get; set; }
    public SponsorshipPackage? Package { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public long ProposedAmountCents { get; set; }
    public string Currency { get; set; } = "cad";
    public string Status { get; set; } = CommunityProgramStatuses.Submitted;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}

public sealed class AnnualCommunityReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? SummaryEn { get; set; }
    public string MetricsJson { get; set; } = "{}";
    public string Status { get; set; } = CommunityProgramStatuses.Draft;
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
}

public sealed class OperationalAutomationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Cadence { get; set; } = "Daily";
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastRunAtUtc { get; set; }
    public DateTime? NextRunAtUtc { get; set; }
    public string? LastStatus { get; set; }
    public string? LastSummary { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
