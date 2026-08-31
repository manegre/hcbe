using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IContentService
{
    Task<ApiResponse<List<PageSectionDto>>> GetPageSectionsAsync(string? page, bool includeInactive = false);
    Task<ApiResponse<List<ServiceContentDto>>> GetServicesAsync(bool includeInactive = false);
    Task<ApiResponse<PageSectionDto>> CreatePageSectionAsync(CreatePageSectionRequest request);
    Task<ApiResponse<PageSectionDto>> UpdatePageSectionAsync(Guid id, UpdatePageSectionRequest request);
    Task<ApiResponse> DeletePageSectionAsync(Guid id);
    Task<ApiResponse<ServiceContentDto>> CreateServiceAsync(CreateServiceContentRequest request);
    Task<ApiResponse<ServiceContentDto>> UpdateServiceAsync(Guid id, UpdateServiceContentRequest request);
    Task<ApiResponse> DeleteServiceAsync(Guid id);
}

