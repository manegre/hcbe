using HcbeApi.Helpers;
using HcbeApi.Models;
namespace HcbeApi.Services;
public interface IMentorshipJourneyService
{
    Task<ApiResponse<MentorshipJourneyDto>> GetAsync(Guid userId, Guid matchId);
    Task<ApiResponse<MentorshipGoalDto>> AddGoalAsync(Guid userId, Guid matchId, CreateMentorshipGoalRequest request);
    Task<ApiResponse<MentorshipGoalDto>> UpdateGoalAsync(Guid userId, Guid goalId, UpdateMentorshipGoalRequest request);
    Task<ApiResponse<MentorshipCheckInDto>> AddCheckInAsync(Guid userId, Guid matchId, CreateMentorshipCheckInRequest request);
    Task<ApiResponse<List<MentorshipCheckInDto>>> GetSupportFlagsAsync();
}
