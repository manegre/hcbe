using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IGrantService
{
    Task<ApiResponse<List<GrantProgramDto>>> GetActiveAsync();
    Task<ApiResponse<List<GrantProgramDto>>> GetAllForAdminAsync();
    Task<ApiResponse<GrantProgramDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<GrantProgramDto>> GetByIdForAdminAsync(Guid id);
    Task<ApiResponse<GrantProgramDto>> CreateAsync(CreateGrantProgramRequest request);
    Task<ApiResponse<GrantProgramDto>> UpdateAsync(Guid id, UpdateGrantProgramRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
    Task<ApiResponse<bool>> ToggleStatusAsync(Guid id);
}
