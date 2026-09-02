using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class EventService : IEventService
{
    private static readonly HashSet<string> AllowedVideoHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be",
        "vimeo.com", "www.vimeo.com", "player.vimeo.com"
    };

    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<EventService> _logger;

    public EventService(
        ApplicationDbContext context,
        INotificationService notificationService,
        IFileStorageService fileStorage,
        ILogger<EventService>? logger = null)
    {
        _context = context;
        _notificationService = notificationService;
        _fileStorage = fileStorage;
        _logger = logger ?? NullLogger<EventService>.Instance;
    }

    public async Task<ApiResponse<List<EventDto>>> GetAllAsync()
    {
        try
        {
            var events = await _context.Events
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .Where(e => e.Status != "Draft" && e.Status != "Cancelled"
                    && e.Status != "Brouillon" && e.Status != "Annulé")
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var eventDtos = events.Select(MapToDto).ToList();
            return ApiResponse<List<EventDto>>.SuccessResponse(eventDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<EventDto>>.ErrorResponse(
                "Failed to retrieve events",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<EventDto>>> GetAllForAdminAsync()
    {
        try
        {
            var events = await _context.Events
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return ApiResponse<List<EventDto>>.SuccessResponse(events.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<EventDto>>.ErrorResponse("Failed to retrieve events", new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<EventDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var eventEntity = await _context.Events
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .FirstOrDefaultAsync(e => e.Id == id
                    && e.Status != "Draft" && e.Status != "Cancelled"
                    && e.Status != "Brouillon" && e.Status != "Annulé");

            if (eventEntity == null)
            {
                return ApiResponse<EventDto>.ErrorResponse("Event not found");
            }

            return ApiResponse<EventDto>.SuccessResponse(MapToDto(eventEntity));
        }
        catch (Exception ex)
        {
            return ApiResponse<EventDto>.ErrorResponse(
                "Failed to retrieve event",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<EventDto>> GetByIdForAdminAsync(Guid id)
    {
        try
        {
            var eventEntity = await _context.Events
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .FirstOrDefaultAsync(e => e.Id == id);
            return eventEntity is null
                ? ApiResponse<EventDto>.ErrorResponse("Event not found")
                : ApiResponse<EventDto>.SuccessResponse(MapToDto(eventEntity));
        }
        catch (Exception ex)
        {
            return ApiResponse<EventDto>.ErrorResponse("Failed to retrieve event", new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<EventDto>> CreateAsync(CreateEventRequest request)
    {
        try
        {
            var eventEntity = new Event
            {
                Title = request.Title,
                TitleEn = NormalizeOptional(request.TitleEn),
                Description = request.Description,
                DescriptionEn = NormalizeOptional(request.DescriptionEn),
                Date = NormalizeUtc(request.Date),
                Location = request.Location,
                LocationEn = NormalizeOptional(request.LocationEn),
                Type = request.Type,
                Zone = request.Zone,
                Capacity = request.Capacity,
                RegistrationDeadline = NormalizeUtc(request.RegistrationDeadline),
                MeetingLink = request.MeetingLink,
                ImageUrl = request.ImageUrl,
                Status = request.Status
            };

            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                "event",
                "Nouvel événement créé",
                eventEntity.Title,
                eventEntity.Id,
                "#events"
            );

            return ApiResponse<EventDto>.SuccessResponse(MapToDto(eventEntity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create event {EventTitle}", request.Title);
            return ApiResponse<EventDto>.ErrorResponse(
                "Failed to create event",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<EventDto>> UpdateAsync(Guid id, UpdateEventRequest request)
    {
        try
        {
            var eventEntity = await _context.Events
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return ApiResponse<EventDto>.ErrorResponse("Event not found");
            }

            if (request.Title != null) eventEntity.Title = request.Title;
            if (request.TitleEn != null) eventEntity.TitleEn = NormalizeOptional(request.TitleEn);
            if (request.Description != null) eventEntity.Description = request.Description;
            if (request.DescriptionEn != null) eventEntity.DescriptionEn = NormalizeOptional(request.DescriptionEn);
            if (request.Date.HasValue) eventEntity.Date = NormalizeUtc(request.Date.Value);
            if (request.Location != null) eventEntity.Location = request.Location;
            if (request.LocationEn != null) eventEntity.LocationEn = NormalizeOptional(request.LocationEn);
            if (request.Type != null) eventEntity.Type = request.Type;
            if (request.Zone != null) eventEntity.Zone = request.Zone;
            if (request.Capacity.HasValue) eventEntity.Capacity = request.Capacity;
            if (request.RegistrationDeadline.HasValue) eventEntity.RegistrationDeadline = NormalizeUtc(request.RegistrationDeadline);
            if (request.MeetingLink != null) eventEntity.MeetingLink = request.MeetingLink;
            if (request.ImageUrl != null) eventEntity.ImageUrl = request.ImageUrl;
            if (request.Status != null) eventEntity.Status = request.Status;

            eventEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<EventDto>.SuccessResponse(MapToDto(eventEntity));
        }
        catch (Exception ex)
        {
            return ApiResponse<EventDto>.ErrorResponse(
                "Failed to update event",
                new List<string> { ex.Message });
        }
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value.HasValue ? NormalizeUtc(value.Value) : null;

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try
        {
            var eventEntity = await _context.Events
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return ApiResponse.CreateError("Event not found");
            }

            foreach (var media in eventEntity.Media.Where(m => m.MediaType == "image"))
            {
                await _fileStorage.DeleteAsync(media.Url);
            }

            foreach (var attachment in eventEntity.Attachments)
            {
                await _fileStorage.DeleteAsync(attachment.Url);
            }

            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();

            return ApiResponse.CreateSuccess("Event deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to delete event",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<EventMediaDto>> AddPhotoAsync(Guid eventId, IFormFile file)
    {
        try
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null)
            {
                return ApiResponse<EventMediaDto>.ErrorResponse("Event not found");
            }

            if (!_fileStorage.IsAllowedImageExtension(file.FileName))
            {
                return ApiResponse<EventMediaDto>.ErrorResponse("Only image files are allowed (jpg, jpeg, png, webp, gif)");
            }

            var (relativeUrl, _) = await _fileStorage.SaveAsync(file, "events");
            var nextOrder = await _context.EventMedia
                .Where(m => m.EventId == eventId)
                .Select(m => (int?)m.DisplayOrder)
                .MaxAsync() ?? -1;

            var media = new EventMedia
            {
                EventId = eventId,
                MediaType = "image",
                Url = relativeUrl,
                FileName = Path.GetFileName(file.FileName),
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType,
                SizeBytes = file.Length,
                DisplayOrder = nextOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.EventMedia.Add(media);
            eventEntity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<EventMediaDto>.SuccessResponse(MapMediaToDto(media));
        }
        catch (Exception ex)
        {
            return ApiResponse<EventMediaDto>.ErrorResponse(
                "Failed to upload photo",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<EventMediaDto>> AddVideoAsync(Guid eventId, AddEventVideoRequest request)
    {
        try
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null)
            {
                return ApiResponse<EventMediaDto>.ErrorResponse("Event not found");
            }

            var url = request.Url?.Trim() ?? string.Empty;
            if (!IsAllowedVideoUrl(url))
            {
                return ApiResponse<EventMediaDto>.ErrorResponse(
                    "Only YouTube or Vimeo HTTPS links are allowed");
            }

            var nextOrder = await _context.EventMedia
                .Where(m => m.EventId == eventId)
                .Select(m => (int?)m.DisplayOrder)
                .MaxAsync() ?? -1;

            var media = new EventMedia
            {
                EventId = eventId,
                MediaType = "video",
                Url = url,
                Caption = string.IsNullOrWhiteSpace(request.Caption) ? null : request.Caption.Trim(),
                CaptionEn = string.IsNullOrWhiteSpace(request.CaptionEn) ? null : request.CaptionEn.Trim(),
                DisplayOrder = nextOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.EventMedia.Add(media);
            eventEntity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<EventMediaDto>.SuccessResponse(MapMediaToDto(media));
        }
        catch (Exception ex)
        {
            return ApiResponse<EventMediaDto>.ErrorResponse(
                "Failed to add video link",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeleteMediaAsync(Guid eventId, Guid mediaId)
    {
        try
        {
            var media = await _context.EventMedia
                .FirstOrDefaultAsync(m => m.Id == mediaId && m.EventId == eventId);

            if (media == null)
            {
                return ApiResponse.CreateError("Media not found");
            }

            if (media.MediaType == "image")
            {
                await _fileStorage.DeleteAsync(media.Url);
            }

            _context.EventMedia.Remove(media);

            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity != null)
            {
                eventEntity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ApiResponse.CreateSuccess("Media deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to delete media",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<EventAttachmentDto>> AddAttachmentAsync(Guid eventId, IFormFile file)
    {
        try
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null)
            {
                return ApiResponse<EventAttachmentDto>.ErrorResponse("Event not found");
            }

            if (!_fileStorage.IsAllowedExtension(file.FileName))
            {
                return ApiResponse<EventAttachmentDto>.ErrorResponse(
                    "File type not allowed. Use PDF, Word, Excel, or image files.");
            }

            var (relativeUrl, _) = await _fileStorage.SaveAsync(file, "events");
            var attachment = new EventAttachment
            {
                EventId = eventId,
                FileName = Path.GetFileName(file.FileName),
                Url = relativeUrl,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType,
                SizeBytes = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            _context.EventAttachments.Add(attachment);
            eventEntity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<EventAttachmentDto>.SuccessResponse(MapAttachmentToDto(attachment));
        }
        catch (Exception ex)
        {
            return ApiResponse<EventAttachmentDto>.ErrorResponse(
                "Failed to upload attachment",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeleteAttachmentAsync(Guid eventId, Guid attachmentId)
    {
        try
        {
            var attachment = await _context.EventAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.EventId == eventId);

            if (attachment == null)
            {
                return ApiResponse.CreateError("Attachment not found");
            }

            await _fileStorage.DeleteAsync(attachment.Url);
            _context.EventAttachments.Remove(attachment);

            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity != null)
            {
                eventEntity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ApiResponse.CreateSuccess("Attachment deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to delete attachment",
                new List<string> { ex.Message });
        }
    }

    private static bool IsAllowedVideoUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        return AllowedVideoHosts.Contains(uri.Host);
    }

    private static EventDto MapToDto(Event eventEntity)
    {
        var media = (eventEntity.Media ?? Enumerable.Empty<EventMedia>())
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.CreatedAt)
            .Select(MapMediaToDto)
            .ToList();

        var attachments = (eventEntity.Attachments ?? Enumerable.Empty<EventAttachment>())
            .OrderBy(a => a.CreatedAt)
            .Select(MapAttachmentToDto)
            .ToList();

        return new EventDto(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.Date,
            eventEntity.Location,
            eventEntity.Type,
            eventEntity.Zone,
            eventEntity.Capacity,
            eventEntity.RegistrationDeadline,
            eventEntity.MeetingLink,
            eventEntity.ImageUrl,
            eventEntity.Status,
            eventEntity.CreatedAt,
            eventEntity.UpdatedAt,
            eventEntity.TitleEn,
            eventEntity.DescriptionEn,
            eventEntity.LocationEn,
            media,
            attachments
        );
    }

    private static EventMediaDto MapMediaToDto(EventMedia media) =>
        new(
            media.Id,
            media.MediaType,
            media.Url,
            media.FileName,
            media.ContentType,
            media.SizeBytes,
            media.Caption,
            media.CaptionEn,
            media.DisplayOrder,
            media.CreatedAt);

    private static EventAttachmentDto MapAttachmentToDto(EventAttachment attachment) =>
        new(
            attachment.Id,
            attachment.FileName,
            attachment.Url,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.CreatedAt);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
