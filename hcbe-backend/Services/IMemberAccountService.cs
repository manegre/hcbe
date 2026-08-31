using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IMemberAccountService
{
    Task<ApiResponse<MemberDto>> GetAsync(Guid userId);
    Task<ApiResponse<MemberDto>> UpdateAsync(Guid userId, UpdateMemberAccountRequest request);
}
