using HcbeApi.Models;
using HcbeApi.Helpers;

namespace HcbeApi.Services;

public interface IAssociationService
{
    Task<ApiResponse<List<AssociationDto>>> GetAllAsync();
    Task<ApiResponse<List<AssociationDto>>> GetAllForAdminAsync();
    Task<ApiResponse<AssociationDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<AssociationDto>> GetByIdForAdminAsync(Guid id);
    Task<ApiResponse<AssociationDto>> CreateAsync(CreateAssociationRequest request);
    Task<ApiResponse<AssociationDto>> UpdateAsync(Guid id, UpdateAssociationRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
    Task<ApiResponse<MediaUploadDto>> UploadImageAsync(Guid id, IFormFile file);
}
