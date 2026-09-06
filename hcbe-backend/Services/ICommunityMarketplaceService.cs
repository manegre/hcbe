using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface ICommunityMarketplaceService
{
    Task<ApiResponse<CommunityOrganizerDto?>> GetMyOrganizerAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<CommunityOrganizerDto>> SaveOrganizerAsync(Guid userId, UpsertOrganizerRequest request, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<CommunityOrganizerDto>>> GetOrganizersAsync(CancellationToken ct);
    Task<ApiResponse<CommunityOrganizerDto>> ReviewOrganizerAsync(Guid id, ReviewOrganizerRequest request, CancellationToken ct);
    Task<ApiResponse<OrganizerOnboardingDto>> CreateOnboardingAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<CommunityOrganizerDto>> RefreshOrganizerAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<OrganizerEventDto>>> GetOrganizerEventsAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<OrganizerEventDto>> SaveOrganizerEventAsync(Guid? id, Guid userId, UpsertOrganizerEventRequest request, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<AdvertisingCampaignDto>>> GetActiveAdsAsync(string placement, string? language, string? province, string? zone, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<AdvertisingCampaignDto>>> GetMyAdsAsync(Guid userId, CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<AdvertisingCampaignDto>>> GetAdsAsync(CancellationToken ct);
    Task<ApiResponse<AdvertisingCampaignDto>> SaveAdAsync(Guid? id, Guid userId, UpsertAdvertisingCampaignRequest request, CancellationToken ct);
    Task<ApiResponse<AdvertisingCampaignDto>> ReviewAdAsync(Guid id, ReviewAdvertisingCampaignRequest request, CancellationToken ct);
    Task<Uri?> TrackAdClickAsync(Guid id, CancellationToken ct);
}

public interface IStripeConnectGateway
{
    bool IsEnabled { get; }
    Task<string> CreateAccountAsync(CommunityOrganizer organizer, CancellationToken ct);
    Task<string> CreateOnboardingLinkAsync(string accountId, string returnUrl, string refreshUrl, CancellationToken ct);
    Task<(bool DetailsSubmitted, bool ChargesEnabled, bool PayoutsEnabled)> GetStatusAsync(string accountId, CancellationToken ct);
}
