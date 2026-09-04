using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetNotificationsAsync(Guid? userId = null, int limit = 5)
    {
        try
        {
            var query = _context.Notifications.AsQueryable();

            // Filter by user if provided, otherwise get notifications for all admins (userId is null)
            if (userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId);
            }
            else
            {
                query = query.Where(n => n.UserId == null);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();

            var notificationDtos = notifications.Select(MapToDto).ToList();
            return ApiResponse<List<NotificationDto>>.SuccessResponse(notificationDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<NotificationDto>>.ErrorResponse(
                "Failed to retrieve notifications",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<NotificationDto>> MarkAsReadAsync(Guid id, Guid? userId = null)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return ApiResponse<NotificationDto>.ErrorResponse("Notification not found");
            }

            if (userId.HasValue && notification.UserId != userId)
            {
                return ApiResponse<NotificationDto>.ErrorResponse("Unauthorized");
            }
            if (!userId.HasValue && notification.UserId.HasValue)
            {
                return ApiResponse<NotificationDto>.ErrorResponse("Unauthorized");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return ApiResponse<NotificationDto>.SuccessResponse(MapToDto(notification));
        }
        catch (Exception ex)
        {
            return ApiResponse<NotificationDto>.ErrorResponse(
                "Failed to mark notification as read",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> MarkAllAsReadAsync(Guid? userId = null)
    {
        try
        {
            var query = _context.Notifications.Where(n => !n.IsRead);

            if (userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId);
            }
            else
            {
                query = query.Where(n => n.UserId == null);
            }

            var unreadNotifications = await query.ToListAsync();
            var now = DateTime.UtcNow;

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            await _context.SaveChangesAsync();

            return ApiResponse.CreateSuccess("All notifications marked as read");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to mark all notifications as read",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<int>> GetUnreadCountAsync(Guid? userId = null)
    {
        try
        {
            var query = _context.Notifications.Where(n => !n.IsRead);

            if (userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId);
            }
            else
            {
                query = query.Where(n => n.UserId == null);
            }

            var count = await query.CountAsync();
            return ApiResponse<int>.SuccessResponse(count);
        }
        catch (Exception ex)
        {
            return ApiResponse<int>.ErrorResponse(
                "Failed to get unread count",
                new List<string> { ex.Message });
        }
    }

    public async Task CreateNotificationAsync(string type, string title, string message, Guid? relatedEntityId = null, string? link = null)
    {
        try
        {
            var notification = new Notification
            {
                Type = type,
                Title = title,
                Message = message,
                RelatedEntityId = relatedEntityId,
                Link = link,
                UserId = null // null means notification is for all admins
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't throw - notification creation failure shouldn't break the main operation
            Console.WriteLine($"Failed to create notification: {ex.Message}");
        }
    }

    public async Task CreateForUserAsync(Guid userId, string type, string title, string message, Guid? relatedEntityId = null, string? link = null)
    {
        _context.Notifications.Add(new Notification
        {
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            Link = link,
            UserId = userId
        });
        await _context.SaveChangesAsync();
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.RelatedEntityId,
            notification.Link,
            notification.IsRead,
            notification.UserId,
            notification.CreatedAt,
            notification.ReadAt
        );
    }
}

