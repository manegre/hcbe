using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface INewsletterService
{
    Task<ApiResponse<object>> SubscribeAsync(SubscribeNewsletterRequest request);
    Task<ApiResponse<List<NewsletterSubscriptionDto>>> GetAllAsync(string? language = null, bool? isActive = null);
    Task<ApiResponse<PagedResult<NewsletterSubscriptionDto>>> SearchAsync(int page, int pageSize, string? search, string? sort, string? language = null, bool? isActive = null);
    Task<ApiResponse<NewsletterSubscriptionDto>> UpdateActiveAsync(Guid id, UpdateNewsletterSubscriptionRequest request);
    Task<ApiResponse<string>> ExportActiveCsvAsync();
    Task<ApiResponse> UnsubscribeAsync(string token, Guid? campaignId = null);
    Task<ApiResponse<List<CommunicationConsentEventDto>>> GetConsentHistoryAsync(int limit = 100);
}
