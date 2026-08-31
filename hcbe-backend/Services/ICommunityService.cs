using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface ICommunityService
{
    Task<ApiResponse<List<MentorshipApplicationDto>>> GetMyApplicationsAsync(Guid userId);
    Task<ApiResponse<MentorshipApplicationDto>> ApplyForMentorshipAsync(Guid userId, CreateMentorshipApplicationRequest request);
    Task<ApiResponse<MentorshipApplicationDto>> WithdrawApplicationAsync(Guid userId, Guid id);
    Task<ApiResponse<List<MentorshipApplicationDto>>> GetApplicationsForAdminAsync(string? role, string? status, string? search);
    Task<ApiResponse<MentorshipApplicationDto>> ReviewApplicationAsync(Guid id, ReviewMentorshipApplicationRequest request);
    Task<ApiResponse<List<MentorshipMatchDto>>> GetMyMatchesAsync(Guid userId);
    Task<ApiResponse<List<MentorshipMatchDto>>> GetMatchesForAdminAsync();
    Task<ApiResponse<MentorshipMatchDto>> CreateMatchAsync(CreateMentorshipMatchRequest request);
    Task<ApiResponse<MentorshipMatchDto>> RespondToMatchAsync(Guid userId, Guid id, string response);
    Task<ApiResponse<MentorshipMatchDto>> UpdateMatchStatusAsync(Guid id, UpdateMentorshipMatchStatusRequest request);
    Task<ApiResponse<NetworkingProfileDto>> GetMyNetworkingProfileAsync(Guid userId);
    Task<ApiResponse<NetworkingProfileDto>> UpsertNetworkingProfileAsync(Guid userId, UpsertNetworkingProfileRequest request);
    Task<ApiResponse<List<NetworkingProfileDto>>> SearchDirectoryAsync(Guid userId, string? search, string? province);
    Task<ApiResponse<ConnectionRequestDto>> CreateConnectionRequestAsync(Guid userId, CreateConnectionRequestRequest request);
    Task<ApiResponse<List<ConnectionRequestDto>>> GetMyConnectionRequestsAsync(Guid userId);
    Task<ApiResponse<ConnectionRequestDto>> RespondToConnectionRequestAsync(Guid userId, Guid id, RespondConnectionRequestRequest request);
}
