using HcbeApi.Helpers;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class PushEndpoints
{
    public static void MapPushEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/push").WithTags("Push notifications").RequireAuthorization("Authenticated");
        group.MapGet("/configuration", (IAppPushService service) => Results.Ok(ApiResponse<WebPushConfigurationDto>.SuccessResponse(service.GetConfiguration())));
        group.MapGet("/status", async (HttpContext http, IAppPushService service) => (await service.GetStatusAsync(http.GetUserId()!.Value)).HandleServiceResponse());
        group.MapPost("/subscriptions", async (WebPushSubscriptionRequest request, HttpContext http, IAppPushService service) =>
            (await service.SubscribeAsync(http.GetUserId()!.Value, request, http.Request.Headers.UserAgent.ToString())).HandleServiceResponse());
        group.MapPost("/unsubscribe", async (WebPushUnsubscribeRequest request, HttpContext http, IAppPushService service) =>
            (await service.UnsubscribeAsync(http.GetUserId()!.Value, request.Endpoint)).HandleServiceResponse());
        group.MapPost("/test", async (WebPushTestRequest request, HttpContext http, IAppPushService service, CancellationToken cancellationToken) =>
            (await service.SendTestAsync(http.GetUserId()!.Value, request.Language ?? "fr", cancellationToken)).HandleServiceResponse());
    }
}

public record WebPushUnsubscribeRequest(string Endpoint);
public record WebPushTestRequest(string? Language);
