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

        group.MapGet("/", async (INotificationService notificationService, HttpContext context) =>
        {
            var userId = context.GetUserId();
            var response = await notificationService.GetNotificationsAsync(userId, limit: 5);
            return response.HandleServiceResponse();
        })
        .WithName("GetNotifications")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse<List<NotificationDto>>>()
        .Produces(400);

        group.MapGet("/unread-count", async (INotificationService notificationService, HttpContext context) =>
        {
            var userId = context.GetUserId();
            var response = await notificationService.GetUnreadCountAsync(userId);
            return response.HandleServiceResponse();
        })
        .WithName("GetUnreadCount")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse<int>>()
        .Produces(400);

        group.MapPut("/{id:guid}/read", async (Guid id, INotificationService notificationService, HttpContext context) =>
        {
            var userId = context.GetUserId();
            var response = await notificationService.MarkAsReadAsync(id, userId);
            return response.HandleServiceResponse();
        })
        .WithName("MarkNotificationAsRead")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse<NotificationDto>>()
        .Produces(404)
        .Produces(400);

        group.MapPut("/mark-all-read", async (INotificationService notificationService, HttpContext context) =>
        {
            var userId = context.GetUserId();
            var response = await notificationService.MarkAllAsReadAsync(userId);
            return response.HandleServiceResponse();
        })
        .WithName("MarkAllNotificationsAsRead")
        .RequireAuthorization("Authenticated")
        .Produces<ApiResponse>()
        .Produces(400);
    }
}

