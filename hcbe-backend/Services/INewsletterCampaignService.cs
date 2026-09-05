using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface INewsletterCampaignService
{
    Task<ApiResponse<List<NewsletterCampaignDto>>> GetAllAsync();
    Task<ApiResponse<NewsletterCampaignDto>> CreateAsync(CreateNewsletterCampaignRequest request, Guid userId);
    Task<ApiResponse<NewsletterCampaignDto>> SendAsync(Guid id, CancellationToken cancellationToken);
    Task<int> ProcessDueAsync(CancellationToken cancellationToken);
    Task TrackOpenAsync(string token, CancellationToken cancellationToken);
}
