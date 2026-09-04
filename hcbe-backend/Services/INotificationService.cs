using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface INotificationService
{
    Task<ApiResponse<List<NotificationDto>>> GetNotificationsAsync(Guid? userId = null, int limit = 5);
    Task<ApiResponse<NotificationDto>> MarkAsReadAsync(Guid id, Guid? userId = null);
    Task<ApiResponse> MarkAllAsReadAsync(Guid? userId = null);
    Task<ApiResponse<int>> GetUnreadCountAsync(Guid? userId = null);
    Task CreateNotificationAsync(string type, string title, string message, Guid? relatedEntityId = null, string? link = null);
    Task CreateForUserAsync(Guid userId, string type, string title, string message, Guid? relatedEntityId = null, string? link = null);
}

