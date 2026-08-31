using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IDocumentService
{
    Task<ApiResponse<List<DocumentDto>>> GetAllAsync();
    Task<ApiResponse<List<DocumentDto>>> GetAllForAdminAsync();
    Task<ApiResponse<DocumentDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<DocumentDto>> GetByIdForAdminAsync(Guid id);
    Task<ApiResponse<DocumentDto>> UploadAsync(
        IFormFile file,
        string name,
        string? description,
        string? icon,
        string? pages,
        string? category,
        int displayOrder,
        string? nameEn = null,
        string? descriptionEn = null,
        string? pagesEn = null,
        string? categoryEn = null);
    Task<ApiResponse<DocumentDto>> UpdateAsync(
        Guid id,
        IFormFile? file,
        string? name,
        string? description,
        string? icon,
        string? pages,
        string? category,
        int? displayOrder,
        bool? isActive,
        string? nameEn = null,
        string? descriptionEn = null,
        string? pagesEn = null,
        string? categoryEn = null);
    Task<ApiResponse> DeleteAsync(Guid id);
    Task<(byte[] fileBytes, string fileName, string contentType)?> GetFileForDownloadAsync(Guid id, bool includeInactive = false);
}
