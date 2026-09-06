using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface ICommunityProgramsService
{
    Task<ApiResponse<IReadOnlyList<CommunityBusinessDto>>> GetDirectoryAsync(string? search, string? category, string? province, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<CommunityBusinessDto>>> GetMyBusinessesAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<CommunityBusinessDto>> SaveBusinessAsync(Guid? id, Guid userId, UpsertCommunityBusinessRequest request, CancellationToken ct);
    Task<ApiResponse<CommunityBusinessDto>> ReviewBusinessAsync(Guid id, ReviewCommunityBusinessRequest request, CancellationToken ct);
    Task<ApiResponse<NewcomerJourneyDto?>> GetJourneyAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<NewcomerJourneyDto>> SaveJourneyAsync(Guid userId, UpsertNewcomerJourneyRequest request, CancellationToken ct);
    Task<ApiResponse<FamilyHouseholdDto?>> GetHouseholdAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<FamilyHouseholdDto>> SaveHouseholdAsync(Guid userId, UpsertFamilyHouseholdRequest request, CancellationToken ct);
    Task<ApiResponse<FamilyHouseholdDto>> AddFamilyMemberAsync(Guid userId, AddFamilyMemberRequest request, CancellationToken ct);
    Task<ApiResponse<FamilyHouseholdDto>> RemoveFamilyMemberAsync(Guid userId, Guid memberId, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<AppointmentSlotDto>>> GetAvailableSlotsAsync(CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<AppointmentBookingDto>>> GetMyBookingsAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<AppointmentBookingDto>> BookAsync(Guid userId, CreateAppointmentBookingRequest request, CancellationToken ct);
    Task<ApiResponse<AppointmentBookingDto>> CancelBookingAsync(Guid userId, Guid id, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<PartnerBenefitDto>>> GetBenefitsAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<PartnerBenefitDto>> ClaimBenefitAsync(Guid userId, Guid id, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<GrantApplicationDto>>> GetMyGrantApplicationsAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<GrantApplicationDto>> ApplyForGrantAsync(Guid userId, CreateGrantApplicationRequest request, CancellationToken ct);
    Task<ApiResponse<GrantApplicationDto>> WithdrawGrantApplicationAsync(Guid userId, Guid id, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<SponsorshipPackageDto>>> GetSponsorshipPackagesAsync(CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<SponsorshipRequestDto>>> GetMySponsorshipRequestsAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<SponsorshipRequestDto>> RequestSponsorshipAsync(Guid userId, CreateSponsorshipRequest request, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<AnnualCommunityReportDto>>> GetPublishedReportsAsync(CancellationToken ct);
    Task<ApiResponse<CommunityProgramsAdminOverviewDto>> GetAdminOverviewAsync(CancellationToken ct);
    Task<ApiResponse<AppointmentOfferingDto>> SaveOfferingAsync(Guid? id, UpsertAppointmentOfferingRequest request, CancellationToken ct);
    Task<ApiResponse<AppointmentSlotDto>> CreateSlotAsync(CreateAppointmentSlotRequest request, CancellationToken ct);
    Task<ApiResponse<PartnerBenefitDto>> SaveBenefitAsync(Guid? id, UpsertPartnerBenefitRequest request, CancellationToken ct);
    Task<ApiResponse<GrantApplicationDto>> ReviewGrantApplicationAsync(Guid id, ReviewGrantApplicationRequest request, CancellationToken ct);
    Task<ApiResponse<SponsorshipPackageDto>> SaveSponsorshipPackageAsync(Guid? id, UpsertSponsorshipPackageRequest request, CancellationToken ct);
    Task<ApiResponse<SponsorshipRequestDto>> ReviewSponsorshipAsync(Guid id, ReviewSponsorshipRequest request, CancellationToken ct);
    Task<ApiResponse<AnnualCommunityReportDto>> GenerateAnnualReportAsync(int year, CancellationToken ct);
    Task<ApiResponse<AnnualCommunityReportDto>> PublishAnnualReportAsync(Guid id, CancellationToken ct);
    Task<ApiResponse<AutomationRuleDto>> UpdateAutomationRuleAsync(Guid id, UpdateAutomationRuleRequest request, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<AutomationRuleDto>>> RunDueAutomationsAsync(bool force, CancellationToken ct);
}
