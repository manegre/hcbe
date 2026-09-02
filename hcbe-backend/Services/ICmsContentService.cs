using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface ICmsContentService
{
    Task<ApiResponse<CmsPublishedBundleDto>> GetPublishedAsync();
    Task<ApiResponse<List<CmsContentItemDto>>> GetAdminItemsAsync(string? page = null);
    Task<ApiResponse<CmsContentItemDto>> UpsertAsync(UpsertCmsContentRequest request, Guid? userId);
    Task<ApiResponse<CmsContentItemDto>> PublishAsync(Guid id, Guid? userId);
    Task<ApiResponse<CmsPublishResultDto>> PublishAllAsync(Guid? userId);
    Task<ApiResponse<List<CmsContentRevisionDto>>> GetRevisionsAsync(Guid id);
    Task<ApiResponse<CmsContentItemDto>> RollbackAsync(Guid id, int version, Guid? userId);
    Task<ApiResponse> DeleteAsync(Guid id);
}

public interface ICmsContentNotifier
{
    Task NotifyPublishedAsync(long version, CancellationToken cancellationToken = default);
}
