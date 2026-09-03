using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IMemberExperienceService
{
    Task<ApiResponse<MemberOnboardingDto>> GetOnboardingAsync(Guid userId);
    Task<ApiResponse<MemberPreferenceDto>> UpdatePreferencesAsync(Guid userId, UpdateMemberPreferenceRequest request);
}
