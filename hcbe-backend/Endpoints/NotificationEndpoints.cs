using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;
using Microsoft.AspNetCore.Http;

namespace HcbeApi.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .WithOpenApi();

        group.MapGet("/", async (int? limit, bool? member, INotificationService notificationService, HttpContext context) =>
        {
            var scope = context.IsAdmin() && member != true ? null : context.GetUserId();
            var response = await notificationService.GetNotificationsAsync(scope, Math.Clamp(limit ?? 30, 1, 100));
            return response.HandleServiceResponse();
        })
        .WithName("GetNotifications")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse<List<NotificationDto>>>()
        .Produces(400);

        group.MapGet("/unread-count", async (bool? member, INotificationService notificationService, HttpContext context) =>
        {
            var scope = context.IsAdmin() && member != true ? null : context.GetUserId();
            var response = await notificationService.GetUnreadCountAsync(scope);
            return response.HandleServiceResponse();
        })
        .WithName("GetUnreadCount")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse<int>>()
        .Produces(400);

        group.MapPut("/{id:guid}/read", async (Guid id, bool? member, INotificationService notificationService, HttpContext context) =>
        {
            var scope = context.IsAdmin() && member != true ? null : context.GetUserId();
            var response = await notificationService.MarkAsReadAsync(id, scope);
            return response.HandleServiceResponse();
        })
        .WithName("MarkNotificationAsRead")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse<NotificationDto>>()
        .Produces(404)
        .Produces(400);

        group.MapPut("/mark-all-read", async (bool? member, INotificationService notificationService, HttpContext context) =>
        {
            var scope = context.IsAdmin() && member != true ? null : context.GetUserId();
            var response = await notificationService.MarkAllAsReadAsync(scope);
            return response.HandleServiceResponse();
        })
        .WithName("MarkAllNotificationsAsRead")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse>()
        .Produces(400);
    }
}

