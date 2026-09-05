using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IMemberService
{
    Task<ApiResponse<List<MemberDto>>> GetAllAsync();
    Task<ApiResponse<PagedResult<MemberDto>>> SearchAsync(int page, int pageSize, string? search, string? sort);
    Task<ApiResponse<MemberDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<MemberDto>> CreateAsync(CreateMemberRequest request);
    Task<ApiResponse<MemberDto>> UpdateAsync(Guid id, UpdateMemberRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
    Task<ApiResponse<MemberDto>> UpdateAdminStatusAsync(Guid id, bool isAdmin);
    Task<ApiResponse<MemberImportResultDto>> ImportAsync(MemberImportRequest request);
    Task<ApiResponse<List<MemberDuplicateCandidateDto>>> FindDuplicatesAsync();
    Task<ApiResponse<MemberDto>> MergeAsync(Guid primaryMemberId, Guid duplicateMemberId);
}

