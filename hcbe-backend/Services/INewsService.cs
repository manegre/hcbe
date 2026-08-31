using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface INewsService
{
    Task<ApiResponse<List<NewsDto>>> GetPublishedAsync();
    Task<ApiResponse<List<NewsDto>>> GetAllForAdminAsync();
    Task<ApiResponse<NewsDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<NewsDto>> GetByIdForAdminAsync(Guid id);
    Task<ApiResponse<NewsDto>> CreateAsync(CreateNewsRequest request);
    Task<ApiResponse<NewsDto>> UpdateAsync(Guid id, CreateNewsRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
    Task<ApiResponse<MediaUploadDto>> UploadCoverImageAsync(Guid id, IFormFile file);
    Task<ApiResponse<NewsAttachmentDto>> AddAttachmentAsync(Guid id, IFormFile file);
    Task<ApiResponse> DeleteAttachmentAsync(Guid newsId, Guid attachmentId);
}
