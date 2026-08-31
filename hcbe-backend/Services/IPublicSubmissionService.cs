using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IPublicSubmissionService
{
    Task<ApiResponse<PublicSubmissionDto>> SubmitAsync(CreatePublicSubmissionRequest request);
    Task<ApiResponse<List<PublicSubmissionDto>>> GetAllAsync(string? type, string? status);
    Task<ApiResponse<PagedResult<PublicSubmissionDto>>> SearchAsync(int page, int pageSize, string? search, string? sort, string? type, string? status);
    Task<ApiResponse<PublicSubmissionDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<PublicSubmissionDto>> UpdateStatusAsync(Guid id, UpdatePublicSubmissionStatusRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
}
