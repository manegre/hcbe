using HcbeApi.Models;
using HcbeApi.Helpers;

namespace HcbeApi.Services;

public interface IFinanceService
{
    Task<ApiResponse<IReadOnlyList<MembershipPlanDto>>> GetPlansAsync(bool admin, CancellationToken cancellationToken);
    Task<ApiResponse<MembershipPlanDto>> CreatePlanAsync(UpsertMembershipPlanRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<MembershipPlanDto>> UpdatePlanAsync(Guid id, UpsertMembershipPlanRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<IReadOnlyList<DonationCampaignDto>>> GetCampaignsAsync(bool admin, CancellationToken cancellationToken);
    Task<ApiResponse<DonationCampaignDto>> CreateCampaignAsync(UpsertDonationCampaignRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<DonationCampaignDto>> UpdateCampaignAsync(Guid id, UpsertDonationCampaignRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<MemberFinanceSummaryDto>> GetMemberSummaryAsync(Guid userId, CancellationToken cancellationToken);
    Task<ApiResponse<CheckoutSessionDto>> CreateMembershipCheckoutAsync(Guid userId, CreateMembershipCheckoutRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<CheckoutSessionDto>> CreateDonationCheckoutAsync(Guid? userId, CreateDonationCheckoutRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<CheckoutResultDto>> GetCheckoutResultAsync(string sessionId, CancellationToken cancellationToken);
    Task<ApiResponse<BillingPortalDto>> CreateBillingPortalAsync(Guid userId, CancellationToken cancellationToken);
    Task<ApiResponse> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken);
    Task<ApiResponse<FinanceDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken);
    Task<ApiResponse<IReadOnlyList<AdminMembershipDto>>> GetMembershipsAsync(string? search, CancellationToken cancellationToken);
    Task<ApiResponse<IReadOnlyList<FinancialTransactionDto>>> GetTransactionsAsync(string? status, string? kind, string? search, CancellationToken cancellationToken);
    Task<ApiResponse<FinancialTransactionDto>> RefundAsync(Guid transactionId, RefundTransactionRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<MembershipStandingDto>> UpdateMembershipAsync(Guid userId, UpdateMembershipStandingRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<MembershipStandingDto>> RenewCommunityMembershipAsync(Guid userId, CancellationToken cancellationToken);
    Task<ApiResponse<MembershipVerificationDto>> VerifyMembershipAsync(string code, CancellationToken cancellationToken);
    Task<FinancialTransaction?> FindReceiptAsync(string token, CancellationToken cancellationToken);
    Task<int> ProcessMembershipRemindersAsync(CancellationToken cancellationToken);
}
