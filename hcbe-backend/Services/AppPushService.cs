using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebPush;

namespace HcbeApi.Services;

public sealed class AppPushService(
    ApplicationDbContext context,
    IOptions<WebPushOptions> options,
    ILogger<AppPushService> logger) : IAppPushService
{
    private readonly WebPushOptions _options = options.Value;

    public WebPushConfigurationDto GetConfiguration() =>
        new(_options.IsConfigured, _options.IsConfigured ? _options.PublicKey : null);

    public async Task<ApiResponse<WebPushSubscriptionStatusDto>> GetStatusAsync(Guid userId)
    {
        var count = await context.WebPushSubscriptions.CountAsync(item => item.UserId == userId);
        return ApiResponse<WebPushSubscriptionStatusDto>.SuccessResponse(new(_options.IsConfigured, count));
    }

    public async Task<ApiResponse<WebPushSubscriptionStatusDto>> SubscribeAsync(Guid userId, WebPushSubscriptionRequest request, string? userAgent)
    {
        if (!_options.IsConfigured)
            return ApiResponse<WebPushSubscriptionStatusDto>.ErrorResponse("Web push is not configured.");
        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(request.P256dh) || string.IsNullOrWhiteSpace(request.Auth))
            return ApiResponse<WebPushSubscriptionStatusDto>.ErrorResponse("Invalid push subscription.");

        var hash = HashEndpoint(request.Endpoint);
        var item = await context.WebPushSubscriptions.SingleOrDefaultAsync(entry => entry.EndpointHash == hash);
        if (item is null)
        {
            item = new WebPushSubscription { EndpointHash = hash, CreatedAtUtc = DateTime.UtcNow };
            context.WebPushSubscriptions.Add(item);
        }
        item.UserId = userId;
        item.Endpoint = request.Endpoint;
        item.P256dh = request.P256dh;
        item.Auth = request.Auth;
        item.DeviceName = DescribeDevice(userAgent);
        item.LastUsedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return await GetStatusAsync(userId);
    }

    public async Task<ApiResponse> UnsubscribeAsync(Guid userId, string endpoint)
    {
        var hash = HashEndpoint(endpoint);
        var item = await context.WebPushSubscriptions.SingleOrDefaultAsync(entry => entry.UserId == userId && entry.EndpointHash == hash);
        if (item is not null)
        {
            context.WebPushSubscriptions.Remove(item);
            await context.SaveChangesAsync();
        }
        return ApiResponse.CreateSuccess("Push subscription removed.");
    }

    public Task<ApiResponse> SendTestAsync(Guid userId, string language, CancellationToken cancellationToken = default)
    {
        var french = !language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        return SendTestCoreAsync(userId,
            french ? "Notifications HCBE activées" : "HCBE notifications enabled",
            french ? "Vous recevrez ici les mises à jour importantes de votre communauté." : "Important community updates will appear here.",
            cancellationToken);
    }

    private async Task<ApiResponse> SendTestCoreAsync(Guid userId, string title, string message, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured) return ApiResponse.CreateError("Web push is not configured.");
        var count = await SendCoreAsync(userId, title, message, "/espace-membre?section=notifications", false, cancellationToken);
        return count > 0 ? ApiResponse.CreateSuccess("Test notification sent.") : ApiResponse.CreateError("No active push subscription.");
    }

    public async Task SendToUserAsync(Guid userId, string title, string message, string? link = null, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured) return;
        await SendCoreAsync(userId, title, message, link ?? "/espace-membre?section=notifications", true, cancellationToken);
    }

    private async Task<int> SendCoreAsync(Guid userId, string title, string message, string link, bool honorPreference, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured) return 0;
        if (honorPreference)
        {
            var optedIn = await context.MemberPreferences.AsNoTracking().AnyAsync(item => item.UserId == userId && item.PushNotifications, cancellationToken);
            if (!optedIn) return 0;
        }

        var subscriptions = await context.WebPushSubscriptions.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        if (subscriptions.Count == 0) return 0;
        var payload = JsonSerializer.Serialize(new { title, body = message, url = link, icon = "/hcbe-app-icon.svg" });
        var vapid = new VapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
        using var client = new WebPushClient();
        var sent = 0;
        foreach (var item in subscriptions)
        {
            try
            {
                await client.SendNotificationAsync(new PushSubscription(item.Endpoint, item.P256dh, item.Auth), payload, vapid, cancellationToken);
                item.LastUsedAtUtc = DateTime.UtcNow;
                sent++;
            }
            catch (WebPushException exception) when (exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
            {
                context.WebPushSubscriptions.Remove(item);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Unable to send web push notification to subscription {SubscriptionId}", item.Id);
            }
        }
        await context.SaveChangesAsync(cancellationToken);
        return sent;
    }

    private static string HashEndpoint(string endpoint) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(endpoint)));
    private static string DescribeDevice(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Unknown device";
        var browser = userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ? "Edge" : userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) ? "Firefox" : userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ? "Chrome" : userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) ? "Safari" : "Browser";
        var system = userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "Android" : userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ? "iOS" : userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ? "Windows" : userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase) ? "macOS" : userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) ? "Linux" : "Device";
        return $"{browser} · {system}";
    }
}
