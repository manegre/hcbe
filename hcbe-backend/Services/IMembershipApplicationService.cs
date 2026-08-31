using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IMembershipApplicationService
{
    Task<ApiResponse<MembershipApplicationDto>> SubmitAsync(CreateMembershipApplicationRequest request);
    Task<ApiResponse<List<MembershipApplicationDto>>> GetAllAsync(MembershipApplicationStatus? status = null);
    Task<ApiResponse<PagedResult<MembershipApplicationDto>>> SearchAsync(int page, int pageSize, string? search, string? sort, MembershipApplicationStatus? status = null);
    Task<ApiResponse<MembershipApplicationDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<MemberDto>> ApproveAsync(Guid id);
    Task<ApiResponse<MembershipApplicationDto>> RejectAsync(Guid id);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}
