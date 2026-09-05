using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IMemberEngagementService
{
    Task<ApiResponse<MemberEngagementDashboardDto>> GetDashboardAsync(Guid userId);
    Task<ApiResponse<List<SavedMemberItemDto>>> GetSavedAsync(Guid userId);
    Task<ApiResponse<SavedMemberItemDto>> SaveAsync(Guid userId, string entityType, Guid entityId);
    Task<ApiResponse> RemoveSavedAsync(Guid userId, string entityType, Guid entityId);
    Task<ApiResponse<List<MemberBlockDto>>> GetBlocksAsync(Guid userId);
    Task<ApiResponse<MemberBlockDto>> BlockAsync(Guid userId, Guid blockedMemberId);
    Task<ApiResponse> UnblockAsync(Guid userId, Guid blockedMemberId);
    Task<int> ProcessEventRemindersAsync(CancellationToken cancellationToken = default);
    Task<int> ProcessWeeklyDigestsAsync(CancellationToken cancellationToken = default);
    Task<int> ProcessLifecycleJourneysAsync(CancellationToken cancellationToken = default);
}
