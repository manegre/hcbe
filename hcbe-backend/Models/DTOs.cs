using System.ComponentModel.DataAnnotations;

namespace HcbeApi.Models;

// Auth DTOs
public record RegisterRequest(
    [Required] [EmailAddress] string Email,
    [Required] [MinLength(6)] string Password,
    string? FirstName,
    string? LastName);

public record LoginRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Password);

public record GoogleLoginRequest([Required] string Credential);

public record RequestPasswordResetRequest([Required] [EmailAddress] string Email);
public record ConfirmPasswordResetRequest(
    [Required] string Token,
    [Required] [MinLength(8)] string Password);

public record AuthResponse(string Token, UserDto User);

public sealed record AuthSession(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAtUtc, User User);

public record UserDto(Guid Id, string Email, string? FirstName, string? LastName, bool IsAdmin, Guid? MemberId);

public record PrivacyRequestDto(
    Guid Id, string Type, string Status, DateTime RequestedAtUtc, DateTime ExecuteAfterUtc,
    DateTime? CancelledAtUtc, DateTime? CompletedAtUtc);

public record AdminUserDto(
    Guid Id, string Email, string? FirstName, string? LastName, bool IsAdmin, DateTime CreatedAt);

public record CreateAdminUserRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password,
    string? FirstName,
    string? LastName);

public record UpdateAdminUserRequest(
    string? FirstName,
    string? LastName,
    [MinLength(6)] string? Password,
    bool? IsAdmin);

// Member DTOs
public record MemberDto(
    Guid Id, string FirstName, string LastName, string Email, string? Phone,
    string? City, string? Province, string? Profession, string? Expertise,
    string? Interests, string? Availability, string? Zone, bool IsAdmin, DateTime CreatedAt);

public record CreateMemberRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required] [EmailAddress] string Email,
    string? Phone,
    string? City,
    string? Province,
    string? Profession,
    string? Expertise,
    string? Interests,
    string? Availability,
    string? Zone);

public record UpdateMemberRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? City,
    string? Province,
    string? Profession,
    string? Expertise,
    string? Interests,
    string? Availability,
    string? Zone,
    bool? IsAdmin);

// Membership Application DTOs
public record MembershipApplicationDto(
    Guid Id, string FirstName, string LastName, string Email, string? Phone,
    string? City, string? Province, string? Profession, string? Expertise,
    string? Motivation, string Status, Guid? MemberId, DateTime CreatedAt, DateTime? ReviewedAt);

public record CreateMembershipApplicationRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required] [EmailAddress] string Email,
    string? Phone,
    string? City,
    string? Province,
    string? Profession,
    string? Expertise,
    [MaxLength(500)] string? Motivation,
    [MinLength(8)] string? Password = null);

public record UpdateMemberAccountRequest(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? City,
    string? Province,
    string? Profession,
    string? Expertise,
    string? Interests,
    string? Availability);

// Public contact, volunteer, and engagement submissions
public record PublicSubmissionDto(
    Guid Id,
    string Type,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Subject,
    string? City,
    string Details,
    string? MetadataJson,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt);

public record CreatePublicSubmissionRequest(
    [Required] string Type,
    [Required] [MaxLength(100)] string FirstName,
    [Required] [MaxLength(100)] string LastName,
    [Required] [EmailAddress] string Email,
    [MaxLength(40)] string? Phone,
    [MaxLength(160)] string? Subject,
    [MaxLength(120)] string? City,
    [Required] [MaxLength(2000)] string Details,
    Dictionary<string, string>? Metadata);

public record UpdatePublicSubmissionStatusRequest([Required] string Status);

// Event DTOs
public record EventMediaDto(
    Guid Id,
    string MediaType,
    string Url,
    string? FileName,
    string? ContentType,
    long? SizeBytes,
    string? Caption,
    string? CaptionEn,
    int DisplayOrder,
    DateTime CreatedAt);

public record AddEventVideoRequest(
    [Required] string Url,
    string? Caption = null,
    string? CaptionEn = null);

public record EventAttachmentDto(
    Guid Id,
    string FileName,
    string Url,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAt);

public record EventDto(
    Guid Id, string Title, string? Description, DateTime Date, string? Location,
    string? Type, string? Zone, int? Capacity, DateTime? RegistrationDeadline,
    string? MeetingLink, string? ImageUrl, string Status, DateTime CreatedAt, DateTime UpdatedAt,
    string? TitleEn, string? DescriptionEn, string? LocationEn,
    List<EventMediaDto> Media,
    List<EventAttachmentDto> Attachments);

public record CreateEventRequest(
    [Required] string Title,
    string? Description,
    [Required] DateTime Date,
    string? Location,
    string? Type,
    string? Zone,
    int? Capacity,
    DateTime? RegistrationDeadline,
    string? MeetingLink,
    string? ImageUrl,
    string Status,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? LocationEn = null);

public record UpdateEventRequest(
    string? Title, string? Description, DateTime? Date, string? Location,
    string? Type, string? Zone, int? Capacity, DateTime? RegistrationDeadline,
    string? MeetingLink, string? ImageUrl, string? Status,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? LocationEn = null);

// News DTOs
public record NewsAttachmentDto(
    Guid Id,
    string FileName,
    string Url,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAt);

public record MediaUploadDto(
    string Url,
    string FileName,
    string ContentType,
    long SizeBytes);

public record NewsDto(
    Guid Id, string Title, string Content, string? Excerpt, string? ImageUrl,
    string ImagePosition,
    string? Author, string? Category, DateTime? PublishedDate, bool IsPinned, string Status,
    DateTime CreatedAt, DateTime UpdatedAt,
    string? TitleEn, string? ContentEn, string? ExcerptEn,
    List<NewsAttachmentDto> Attachments);

public record CreateNewsRequest(
    [Required] string Title,
    [Required] string Content,
    string? Excerpt,
    string? ImageUrl,
    string? Author,
    string? Category,
    DateTime? PublishedDate,
    bool IsPinned = false,
    string Status = "published",
    string? TitleEn = null,
    string? ContentEn = null,
    string? ExcerptEn = null,
    string? ImagePosition = null);

// Project DTOs
public record ProjectDto(
    Guid Id, string Title, string Location, string Type, string Status,
    int Progress, string Description, string? ImageUrl, string Budget,
    string FundsRaised, string Beneficiaries, DateTime? StartDate, DateTime? EndDate,
    List<string> Partners, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? LocationEn = null,
    string? BeneficiariesEn = null);

public record CreateProjectRequest(
    [Required] string Title,
    [Required] string Location,
    [Required] string Type,
    [Required] string Status,
    [Range(0, 100)] int Progress,
    string Description,
    string? ImageUrl,
    string Budget,
    string FundsRaised,
    string Beneficiaries,
    DateTime? StartDate,
    DateTime? EndDate,
    List<string>? Partners,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? LocationEn = null,
    string? BeneficiariesEn = null);

public record UpdateProjectRequest(
    string? Title,
    string? Location,
    string? Type,
    string? Status,
    [Range(0, 100)] int? Progress,
    string? Description,
    string? ImageUrl,
    string? Budget,
    string? FundsRaised,
    string? Beneficiaries,
    DateTime? StartDate,
    DateTime? EndDate,
    List<string>? Partners,
    bool? IsActive,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? LocationEn = null,
    string? BeneficiariesEn = null);

public record UpdateProjectProgressRequest([Range(0, 100)] int Progress);

// Association DTOs
public record AssociationDto(
    Guid Id, string Name, string? Description, string Province, string City,
    string? Contact, string? Phone, string? President, string? MemberCount,
    int? FoundedYear, string? ImageUrl, string? Website, List<string> Domains,
    bool IsActive, DateTime CreatedAt, DateTime UpdatedAt,
    string? NameEn = null, string? DescriptionEn = null, List<string>? DomainsEn = null);

public record CreateAssociationRequest(
    [Required] string Name,
    string? Description,
    [Required] string Province,
    [Required] string City,
    string? Contact,
    string? Phone,
    string? President,
    string? MemberCount,
    int? FoundedYear,
    string? ImageUrl,
    string? Website,
    List<string> Domains,
    string? NameEn = null,
    string? DescriptionEn = null,
    List<string>? DomainsEn = null);

public record UpdateAssociationRequest(
    string? Name, string? Description, string? Province, string? City,
    string? Contact, string? Phone, string? President, string? MemberCount,
    int? FoundedYear, string? ImageUrl, string? Website, List<string>? Domains,
    bool? IsActive,
    string? NameEn = null,
    string? DescriptionEn = null,
    List<string>? DomainsEn = null);

// Document DTOs
public record DocumentDto(
    Guid Id, string Name, string? Description, string? Icon, string? Type,
    string? Size, string? Pages, string? Category, string? Url, int Downloads,
    bool IsActive, int DisplayOrder, DateTime CreatedAt,
    string? NameEn = null,
    string? DescriptionEn = null,
    string? PagesEn = null,
    string? CategoryEn = null);

// Page Section DTOs
public record PageSectionDto(
    Guid Id, string Page, string Section, string? Title, string? Content,
    bool IsActive, int? DisplayOrder, DateTime CreatedAt, DateTime UpdatedAt,
    string? TitleEn = null, string? ContentEn = null);

public record CreatePageSectionRequest(
    [Required] string Page,
    [Required] string Section,
    string? Title,
    string? Content,
    bool IsActive = true,
    int? DisplayOrder = null,
    string? TitleEn = null,
    string? ContentEn = null);

public record UpdatePageSectionRequest(
    string? Title,
    string? Content,
    bool? IsActive,
    int? DisplayOrder = null,
    string? TitleEn = null,
    string? ContentEn = null);

// Service Content DTOs
public record ServiceContentDto(
    Guid Id, string Title, string? Description, string? Icon, string? Category,
    bool IsActive, int? DisplayOrder, string? Details, string? ExtendedInfo,
    DateTime CreatedAt, DateTime UpdatedAt,
    string? TitleEn = null, string? DescriptionEn = null, string? CategoryEn = null,
    string? DetailsEn = null, string? ExtendedInfoEn = null);

public record CreateServiceContentRequest(
    [Required] string Title,
    string? Description,
    string? Icon,
    string? Category,
    bool IsActive = true,
    int? DisplayOrder = null,
    string? Details = null,
    string? ExtendedInfo = null,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? CategoryEn = null,
    string? DetailsEn = null,
    string? ExtendedInfoEn = null);

public record UpdateServiceContentRequest(
    string? Title,
    string? Description,
    string? Icon,
    string? Category,
    bool? IsActive,
    string? Details,
    string? ExtendedInfo,
    int? DisplayOrder = null,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? CategoryEn = null,
    string? DetailsEn = null,
    string? ExtendedInfoEn = null);

// Partner CMS DTOs
public record PartnerDto(
    Guid Id,
    string Name,
    string? NameEn,
    string? Description,
    string? DescriptionEn,
    string? LogoUrl,
    string? WebsiteUrl,
    string? AltText,
    string? AltTextEn,
    bool IsFeatured,
    bool IsActive,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreatePartnerRequest(
    [Required, MaxLength(160)] string Name,
    [MaxLength(160)] string? NameEn = null,
    [MaxLength(600)] string? Description = null,
    [MaxLength(600)] string? DescriptionEn = null,
    [MaxLength(1000)] string? LogoUrl = null,
    [MaxLength(1000)] string? WebsiteUrl = null,
    [MaxLength(220)] string? AltText = null,
    [MaxLength(220)] string? AltTextEn = null,
    bool IsFeatured = true,
    bool IsActive = true,
    int DisplayOrder = 0);

public record UpdatePartnerRequest(
    [MaxLength(160)] string? Name = null,
    [MaxLength(160)] string? NameEn = null,
    [MaxLength(600)] string? Description = null,
    [MaxLength(600)] string? DescriptionEn = null,
    [MaxLength(1000)] string? LogoUrl = null,
    [MaxLength(1000)] string? WebsiteUrl = null,
    [MaxLength(220)] string? AltText = null,
    [MaxLength(220)] string? AltTextEn = null,
    bool? IsFeatured = null,
    bool? IsActive = null,
    int? DisplayOrder = null);

public record ReorderPartnersRequest([Required] List<Guid> PartnerIds);

// Statistic DTOs
public record StatisticDto(
    Guid Id, string Key, string Value, string Label, int DisplayOrder,
    DateTime CreatedAt, DateTime UpdatedAt);

// Navigation Item DTOs
public record NavigationItemDto(
    Guid Id, string Label, string Url, bool IsActive, int DisplayOrder,
    DateTime CreatedAt, DateTime UpdatedAt, string? LabelEn = null);

public record CreateNavigationItemRequest(
    [Required] string Label,
    [Required] string Url,
    bool IsActive = true,
    int DisplayOrder = 0,
    string? LabelEn = null);

public record UpdateNavigationItemRequest(
    string? Label,
    string? Url,
    bool? IsActive,
    int? DisplayOrder,
    string? LabelEn = null);

// Footer Link DTOs
public record FooterLinkDto(
    Guid Id, string Category, string Label, string Url, bool IsActive,
    int DisplayOrder, DateTime CreatedAt, DateTime UpdatedAt,
    string? CategoryEn = null, string? LabelEn = null);

public record CreateFooterLinkRequest(
    [Required] string Category,
    [Required] string Label,
    [Required] string Url,
    bool IsActive = true,
    int DisplayOrder = 0,
    string? CategoryEn = null,
    string? LabelEn = null);

public record UpdateFooterLinkRequest(
    string? Category,
    string? Label,
    string? Url,
    bool? IsActive,
    int? DisplayOrder,
    string? CategoryEn = null,
    string? LabelEn = null);

// Site Setting DTOs
public record SiteSettingDto(
    Guid Id, string Key, string Value, string? Description,
    DateTime CreatedAt, DateTime UpdatedAt);

// Notification DTOs
public record NotificationDto(
    Guid Id, string Type, string Title, string Message, Guid? RelatedEntityId,
    string? Link, bool IsRead, Guid? UserId, DateTime CreatedAt, DateTime? ReadAt);

public record MarkNotificationReadRequest(bool IsRead);

// Team Member DTOs
public record TeamMemberDto(
    Guid Id, string Name, string Position, string Region, string Zone,
    string? Photo, string? Bio, string? Email, bool IsActive, int Order,
    DateTime CreatedAt, DateTime UpdatedAt,
    string? PositionEn = null, string? RegionEn = null, string? ZoneEn = null, string? BioEn = null);

public record CreateTeamMemberRequest(
    [Required] string Name,
    [Required] string Position,
    [Required] string Region,
    [Required] string Zone,
    string? Photo,
    string? Bio,
    string? Email,
    int Order = 0,
    bool IsActive = true,
    string? PositionEn = null,
    string? RegionEn = null,
    string? ZoneEn = null,
    string? BioEn = null);

public record UpdateTeamMemberRequest(
    string? Name,
    string? Position,
    string? Region,
    string? Zone,
    string? Photo,
    string? Bio,
    string? Email,
    bool? IsActive,
    int? Order,
    string? PositionEn = null,
    string? RegionEn = null,
    string? ZoneEn = null,
    string? BioEn = null);

// Grant Program DTOs
public record GrantProgramDto(
    Guid Id, string Title, string Description, string Icon, string Amount, string Duration,
    List<string> EligibilityCriteria, string? ApplicationUrl, int DisplayOrder, bool IsActive,
    DateTime CreatedAt, DateTime UpdatedAt,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? AmountEn = null,
    string? DurationEn = null,
    List<string>? EligibilityCriteriaEn = null);

public record CreateGrantProgramRequest(
    [Required] string Title,
    [Required] string Description,
    string Icon,
    [Required] string Amount,
    [Required] string Duration,
    List<string>? EligibilityCriteria,
    string? ApplicationUrl,
    int DisplayOrder = 0,
    bool IsActive = true,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? AmountEn = null,
    string? DurationEn = null,
    List<string>? EligibilityCriteriaEn = null);

public record UpdateGrantProgramRequest(
    string? Title,
    string? Description,
    string? Icon,
    string? Amount,
    string? Duration,
    List<string>? EligibilityCriteria,
    string? ApplicationUrl,
    int? DisplayOrder,
    bool? IsActive,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? AmountEn = null,
    string? DurationEn = null,
    List<string>? EligibilityCriteriaEn = null);

// Consultation DTOs
public record ConsultationDto(
    Guid Id, string Title, string Description, string Icon, string LayoutType,
    string? ActionUrl, string? ActionLabel, string? SecondaryActionUrl, string? SecondaryActionLabel,
    string AccentColor, int DisplayOrder, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? ActionLabelEn = null,
    string? SecondaryActionLabelEn = null);

public record CreateConsultationRequest(
    [Required] string Title,
    [Required] string Description,
    string Icon,
    string LayoutType,
    string? ActionUrl,
    string? ActionLabel,
    string? SecondaryActionUrl,
    string? SecondaryActionLabel,
    string AccentColor,
    int DisplayOrder = 0,
    bool IsActive = true,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? ActionLabelEn = null,
    string? SecondaryActionLabelEn = null);

public record UpdateConsultationRequest(
    string? Title,
    string? Description,
    string? Icon,
    string? LayoutType,
    string? ActionUrl,
    string? ActionLabel,
    string? SecondaryActionUrl,
    string? SecondaryActionLabel,
    string? AccentColor,
    int? DisplayOrder,
    bool? IsActive,
    string? TitleEn = null,
    string? DescriptionEn = null,
    string? ActionLabelEn = null,
    string? SecondaryActionLabelEn = null);

// Newsletter DTOs
public record NewsletterSubscriptionDto(
    Guid Id,
    string Email,
    string FullName,
    string PreferredLanguage,
    DateTime ConsentAcceptedAt,
    bool IsActive,
    string Source,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SubscribeNewsletterRequest(
    [Required] [EmailAddress] string Email,
    [Required] [MaxLength(150)] string FullName,
    [Required] string PreferredLanguage,
    [Required] bool ConsentAccepted,
    [Required] string Source);

public record UpdateNewsletterSubscriptionRequest(
    [Required] bool IsActive);

public record NewsletterCampaignDto(
    Guid Id, string Subject, string? SubjectEn, string Body, string? BodyEn,
    string Status, int RecipientCount, int SentCount, int FailedCount,
    string? LastError, DateTime CreatedAt, DateTime? SentAt);

public record CreateNewsletterCampaignRequest(
    [Required] [MaxLength(200)] string Subject,
    [MaxLength(200)] string? SubjectEn,
    [Required] [MaxLength(20000)] string Body,
    [MaxLength(20000)] string? BodyEn);

// Mentorship and member networking
public record MentorshipApplicationDto(
    Guid Id, Guid MemberId, string MemberName, string? MemberEmail, string Role,
    string ProfessionalSummary, string Expertise, string Objectives, string Availability,
    string PreferredLanguage, bool ConsentToShare, string Status, string? CommitteeNotes,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime? ReviewedAt);

public record CreateMentorshipApplicationRequest(
    [Required] string Role,
    [Required] [StringLength(800, MinimumLength = 20)] string ProfessionalSummary,
    [Required] [StringLength(800, MinimumLength = 10)] string Expertise,
    [Required] [StringLength(1200, MinimumLength = 20)] string Objectives,
    [Required] [StringLength(300)] string Availability,
    [Required] string PreferredLanguage,
    bool ConsentToShare);

public record ReviewMentorshipApplicationRequest(
    [Required] string Status,
    [StringLength(1200)] string? CommitteeNotes);

public record MentorshipMatchDto(
    Guid Id, Guid MentorApplicationId, Guid MenteeApplicationId,
    string MentorName, string MenteeName, string Status,
    bool MentorAccepted, bool MenteeAccepted, string? CommitteeNotes,
    string? CounterpartName, string? CounterpartEmail,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime? ActivatedAt, DateTime? CompletedAt);

public record CreateMentorshipMatchRequest(
    Guid MentorApplicationId,
    Guid MenteeApplicationId,
    [StringLength(1200)] string? CommitteeNotes);

public record UpdateMentorshipMatchStatusRequest([Required] string Status);

public record NetworkingProfileDto(
    Guid Id, Guid MemberId, string MemberName, string Headline, string Bio,
    string Expertise, string Sectors, string? City, string? Province,
    bool IsVisible, bool AllowContactRequests, DateTime UpdatedAt);

public record UpsertNetworkingProfileRequest(
    [Required] [StringLength(160, MinimumLength = 5)] string Headline,
    [Required] [StringLength(1200, MinimumLength = 20)] string Bio,
    [Required] [StringLength(600, MinimumLength = 5)] string Expertise,
    [Required] [StringLength(400, MinimumLength = 2)] string Sectors,
    [StringLength(120)] string? City,
    [StringLength(120)] string? Province,
    bool IsVisible,
    bool AllowContactRequests);

public record CreateConnectionRequestRequest(
    Guid RecipientMemberId,
    [Required] [StringLength(600, MinimumLength = 10)] string Message);

public record ConnectionRequestDto(
    Guid Id, Guid RequesterMemberId, Guid RecipientMemberId,
    string RequesterName, string RecipientName, string Message, string Status,
    string Direction, string? SharedEmail, DateTime CreatedAt, DateTime? RespondedAt);

public record RespondConnectionRequestRequest([Required] string Status);

public record ConversationDto(
    Guid Id, Guid CounterpartMemberId, string CounterpartName, string RelationshipType,
    string Status, string? LastMessage, DateTime? LastMessageAt, int UnreadCount, DateTime CreatedAt);

public record MessagingContactDto(
    Guid MemberId, string MemberName, string RelationshipType, Guid RelationshipId,
    bool HasConversation, Guid? ConversationId);

public record StartConversationRequest(Guid MemberId);

public record PrivateMessageDto(
    Guid Id, Guid ConversationId, Guid SenderMemberId, string SenderName,
    string Body, bool IsMine, DateTime CreatedAt, DateTime? ReadAt);

public record SendPrivateMessageRequest(
    [Required] [StringLength(2000, MinimumLength = 1)] string Body);

public record ReportConversationRequest(
    [Required] [StringLength(1000, MinimumLength = 10)] string Reason);

public record ConversationReportDto(
    Guid Id, Guid ConversationId, Guid ReporterMemberId, string ReporterName,
    string MemberOneName, string MemberTwoName, string Reason, string Status,
    string? AdminNotes, DateTime CreatedAt, DateTime? ResolvedAt);

public record ResolveConversationReportRequest(
    [Required] string Status,
    [StringLength(1200)] string? AdminNotes,
    bool SuspendConversation);

