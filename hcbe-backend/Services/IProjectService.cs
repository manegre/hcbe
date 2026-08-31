using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IProjectService
{
    Task<ApiResponse<List<ProjectDto>>> GetAllAsync();
    Task<ApiResponse<List<ProjectDto>>> GetAllForAdminAsync();
    Task<ApiResponse<ProjectDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<ProjectDto>> GetByIdForAdminAsync(Guid id);
    Task<ApiResponse<ProjectDto>> CreateAsync(CreateProjectRequest request);
    Task<ApiResponse<ProjectDto>> UpdateAsync(Guid id, UpdateProjectRequest request);
    Task<ApiResponse<ProjectDto>> UpdateProgressAsync(Guid id, int progress);
    Task<ApiResponse> DeleteAsync(Guid id);
}

