using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IUserAdminService
{
    Task<ApiResponse<List<AdminUserDto>>> GetAdminUsersAsync();
    Task<ApiResponse<AdminUserDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<AdminUserDto>> CreateAdminUserAsync(CreateAdminUserRequest request);
    Task<ApiResponse<AdminUserDto>> UpdateAsync(Guid id, UpdateAdminUserRequest request, Guid currentUserId);
    Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid currentUserId);
}
