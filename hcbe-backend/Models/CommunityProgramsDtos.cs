using System.ComponentModel.DataAnnotations;

namespace HcbeApi.Models;

public sealed record CommunityBusinessDto(Guid Id, string Name, string? NameEn, string Category,
    string Description, string? DescriptionEn, string? Services, string? ServicesEn,
    string ContactEmail, string? ContactPhone, string? WebsiteUrl, string? LogoUrl,
    string? City, string? Province, string? ServiceRegions, string Status, bool IsFeatured,
    string? ReviewNotes, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record UpsertCommunityBusinessRequest([Required, MaxLength(180)] string Name,
    [MaxLength(180)] string? NameEn, [Required, MaxLength(80)] string Category,
    [Required, StringLength(3000, MinimumLength = 20)] string Description,
    [MaxLength(3000)] string? DescriptionEn, [MaxLength(1000)] string? Services,
    [MaxLength(1000)] string? ServicesEn, [Required, EmailAddress, MaxLength(320)] string ContactEmail,
    [MaxLength(40)] string? ContactPhone, [MaxLength(1000)] string? WebsiteUrl,
    [MaxLength(1000)] string? LogoUrl, [MaxLength(120)] string? City,
    [MaxLength(120)] string? Province, [MaxLength(500)] string? ServiceRegions);
public sealed record ReviewCommunityBusinessRequest([Required, MaxLength(30)] string Status,
    bool IsFeatured, [MaxLength(2000)] string? ReviewNotes);

public sealed record NewcomerJourneyDto(Guid Id, DateOnly? ArrivalDate, string? City, string? Province,
    string PreferredLanguage, IReadOnlyList<string> Needs, IReadOnlyList<string> CompletedSteps,
    bool MentorRequested, int ProgressPercent, DateTime UpdatedAtUtc);
public sealed record UpsertNewcomerJourneyRequest(DateOnly? ArrivalDate, [MaxLength(120)] string? City,
    [MaxLength(120)] string? Province, [Required, MaxLength(5)] string PreferredLanguage,
    IReadOnlyList<string>? Needs, IReadOnlyList<string>? CompletedSteps, bool MentorRequested);

public sealed record FamilyHouseholdMemberDto(Guid Id, string FullName, string Relationship,
    string? Email, DateOnly? BirthDate, string Status, DateTime CreatedAtUtc);
public sealed record FamilyHouseholdDto(Guid Id, string HouseholdName, string Status,
    IReadOnlyList<FamilyHouseholdMemberDto> Members, DateTime UpdatedAtUtc);
public sealed record UpsertFamilyHouseholdRequest([Required, MaxLength(160)] string HouseholdName);
public sealed record AddFamilyMemberRequest([Required, MaxLength(160)] string FullName,
    [Required, MaxLength(60)] string Relationship, [EmailAddress, MaxLength(320)] string? Email,
    DateOnly? BirthDate);

public sealed record AppointmentOfferingDto(Guid Id, string Title, string? TitleEn, string Description,
    string? DescriptionEn, string Category, string Mode, string? Location, string? LocationEn,
    int DurationMinutes, bool IsActive);
public sealed record AppointmentSlotDto(Guid Id, Guid OfferingId, string OfferingTitle,
    string? OfferingTitleEn, DateTime StartsAtUtc, DateTime EndsAtUtc, int Capacity,
    int Available, bool IsCancelled);
public sealed record AppointmentBookingDto(Guid Id, Guid SlotId, string OfferingTitle,
    string? OfferingTitleEn, DateTime StartsAtUtc, DateTime EndsAtUtc, string? Reason,
    string Status, DateTime CreatedAtUtc);
public sealed record UpsertAppointmentOfferingRequest([Required, MaxLength(180)] string Title,
    [MaxLength(180)] string? TitleEn, [Required, MaxLength(2000)] string Description,
    [MaxLength(2000)] string? DescriptionEn, [Required, MaxLength(80)] string Category,
    [Required, MaxLength(20)] string Mode, [MaxLength(300)] string? Location,
    [MaxLength(300)] string? LocationEn, [Range(15, 240)] int DurationMinutes, bool IsActive);
public sealed record CreateAppointmentSlotRequest(Guid OfferingId, DateTime StartsAtUtc,
    DateTime EndsAtUtc, [Range(1, 100)] int Capacity);
public sealed record CreateAppointmentBookingRequest(Guid SlotId, [MaxLength(1000)] string? Reason);

public sealed record PartnerBenefitDto(Guid Id, Guid PartnerId, string PartnerName, string? PartnerLogoUrl,
    string Title, string? TitleEn, string Description, string? DescriptionEn, string? Terms,
    string? TermsEn, string? RedemptionInstructions, string? RedemptionInstructionsEn,
    DateTime? StartsAtUtc, DateTime? EndsAtUtc, int? MaxClaims, int ClaimCount,
    bool IsActive, bool IsClaimed, string? RedemptionCode);
public sealed record UpsertPartnerBenefitRequest(Guid PartnerId, [Required, MaxLength(180)] string Title,
    [MaxLength(180)] string? TitleEn, [Required, MaxLength(2500)] string Description,
    [MaxLength(2500)] string? DescriptionEn, [MaxLength(2000)] string? Terms,
    [MaxLength(2000)] string? TermsEn, [MaxLength(2000)] string? RedemptionInstructions,
    [MaxLength(2000)] string? RedemptionInstructionsEn, [MaxLength(100)] string? SharedCode,
    DateTime? StartsAtUtc, DateTime? EndsAtUtc, [Range(1, 100000)] int? MaxClaims, bool IsActive);

public sealed record GrantApplicationDto(Guid Id, Guid GrantProgramId, string ProgramTitle,
    string? ProgramTitleEn, string ApplicantName, string ApplicantEmail, string Statement,
    IReadOnlyDictionary<string, string> Answers, IReadOnlyList<string> Documents,
    string Status, string? AdminNotes, DateTime SubmittedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreateGrantApplicationRequest(Guid GrantProgramId,
    [Required, MaxLength(160)] string ApplicantName, [Required, EmailAddress, MaxLength(320)] string ApplicantEmail,
    [Required, StringLength(5000, MinimumLength = 50)] string Statement,
    IReadOnlyDictionary<string, string>? Answers, IReadOnlyList<string>? Documents);
public sealed record ReviewGrantApplicationRequest([Required, MaxLength(30)] string Status,
    [MaxLength(3000)] string? AdminNotes);

public sealed record SponsorshipPackageDto(Guid Id, string Title, string? TitleEn, string Description,
    string? DescriptionEn, IReadOnlyList<string> Deliverables, long AmountCents, string Currency,
    bool IsActive, int DisplayOrder);
public sealed record UpsertSponsorshipPackageRequest([Required, MaxLength(180)] string Title,
    [MaxLength(180)] string? TitleEn, [Required, MaxLength(2500)] string Description,
    [MaxLength(2500)] string? DescriptionEn, IReadOnlyList<string>? Deliverables,
    [Range(0, 100_000_000)] long AmountCents, [Required, MaxLength(3)] string Currency,
    bool IsActive, int DisplayOrder);
public sealed record SponsorshipRequestDto(Guid Id, Guid? PackageId, string? PackageTitle,
    string OrganizationName, string ContactEmail, string Objective, string? Notes,
    long ProposedAmountCents, string Currency, string Status, DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
public sealed record CreateSponsorshipRequest(Guid? PackageId,
    [Required, MaxLength(180)] string OrganizationName, [Required, EmailAddress, MaxLength(320)] string ContactEmail,
    [Required, StringLength(3000, MinimumLength = 20)] string Objective,
    [Range(0, 100_000_000)] long ProposedAmountCents, [Required, MaxLength(3)] string Currency);
public sealed record ReviewSponsorshipRequest([Required, MaxLength(30)] string Status,
    [MaxLength(3000)] string? Notes);

public sealed record AnnualCommunityReportDto(Guid Id, int Year, string Title, string? TitleEn,
    string Summary, string? SummaryEn, IReadOnlyDictionary<string, decimal> Metrics,
    string Status, DateTime GeneratedAtUtc, DateTime? PublishedAtUtc);
public sealed record AutomationRuleDto(Guid Id, string Key, string Name, string NameEn,
    string Cadence, bool IsEnabled, DateTime? LastRunAtUtc, DateTime? NextRunAtUtc,
    string? LastStatus, string? LastSummary);
public sealed record UpdateAutomationRuleRequest(bool IsEnabled, [Required, MaxLength(30)] string Cadence);

public sealed record CommunityProgramsAdminOverviewDto(
    IReadOnlyList<CommunityBusinessDto> Businesses,
    IReadOnlyList<AppointmentOfferingDto> Offerings,
    IReadOnlyList<AppointmentSlotDto> Slots,
    IReadOnlyList<AppointmentBookingDto> Bookings,
    IReadOnlyList<PartnerBenefitDto> Benefits,
    IReadOnlyList<GrantApplicationDto> GrantApplications,
    IReadOnlyList<SponsorshipPackageDto> SponsorshipPackages,
    IReadOnlyList<SponsorshipRequestDto> SponsorshipRequests,
    IReadOnlyList<AnnualCommunityReportDto> AnnualReports,
    IReadOnlyList<AutomationRuleDto> AutomationRules);
