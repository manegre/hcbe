using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface ITeamMemberService
{
    Task<ApiResponse<List<TeamMemberDto>>> GetAllAsync();
    Task<ApiResponse<List<TeamMemberDto>>> GetActiveAsync();
    Task<ApiResponse<TeamMemberDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<TeamMemberDto>> CreateAsync(CreateTeamMemberRequest request);
    Task<ApiResponse<TeamMemberDto>> UpdateAsync(Guid id, UpdateTeamMemberRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
    Task<ApiResponse<bool>> ToggleStatusAsync(Guid id);
}
