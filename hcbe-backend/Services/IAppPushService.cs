using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";
    public string Subject { get; set; } = "mailto:contact@hcbe.ca";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}

public record WebPushConfigurationDto(bool Enabled, string? PublicKey);
public record WebPushSubscriptionRequest(string Endpoint, string P256dh, string Auth);
public record WebPushSubscriptionStatusDto(bool Configured, int DeviceCount);

public interface IAppPushService
{
    WebPushConfigurationDto GetConfiguration();
    Task<ApiResponse<WebPushSubscriptionStatusDto>> GetStatusAsync(Guid userId);
    Task<ApiResponse<WebPushSubscriptionStatusDto>> SubscribeAsync(Guid userId, WebPushSubscriptionRequest request, string? userAgent);
    Task<ApiResponse> UnsubscribeAsync(Guid userId, string endpoint);
    Task<ApiResponse> SendTestAsync(Guid userId, string language, CancellationToken cancellationToken = default);
    Task<int> SendToUserAsync(Guid userId, string title, string message, string? link = null, CancellationToken cancellationToken = default);
}
