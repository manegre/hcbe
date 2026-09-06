using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class EventService : IEventService
{
    private const int MaxSpeakers = 20;
    private const int MaxOrganizers = 20;
    private const int MaxSpeakerNameLength = 160;
    private const string DefaultTimeZone = "America/Toronto";

    private static readonly HashSet<string> AllowedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "InPerson", "Online", "Hybrid"
    };

    private static readonly HashSet<string> AllowedRegistrationModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Disabled", "External", "Native"
    };

    private static readonly HashSet<string> AllowedSalesModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "HCBE", "Community"
    };

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
                .Include(e => e.Speakers)
                .Include(e => e.Organizers)
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .Include(e => e.Registrations)
                .Include(e => e.CommunityOrganizer)
                .AsSplitQuery()
                .Where(e => e.Status != "Draft" && e.Status != "Cancelled"
                    && e.Status != "Brouillon" && e.Status != "Annulé")
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var eventDtos = events.Select(eventEntity => MapToDto(eventEntity)).ToList();
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
                .Include(e => e.Speakers)
                .Include(e => e.Organizers)
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .Include(e => e.Registrations)
                .Include(e => e.CommunityOrganizer)
                .AsSplitQuery()
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return ApiResponse<List<EventDto>>.SuccessResponse(events.Select(eventEntity => MapToDto(eventEntity, true)).ToList());
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
                .Include(e => e.Speakers)
                .Include(e => e.Organizers)
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .Include(e => e.Registrations)
                .Include(e => e.CommunityOrganizer)
                .AsSplitQuery()
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
                .Include(e => e.Speakers)
                .Include(e => e.Organizers)
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .Include(e => e.Registrations)
                .Include(e => e.CommunityOrganizer)
                .AsSplitQuery()
                .FirstOrDefaultAsync(e => e.Id == id);
            return eventEntity is null
                ? ApiResponse<EventDto>.ErrorResponse("Event not found")
                : ApiResponse<EventDto>.SuccessResponse(MapToDto(eventEntity, true));
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
            var speakerValidation = NormalizeSpeakers(request.Speakers);
            if (speakerValidation.Error is not null)
            {
                return ApiResponse<EventDto>.ErrorResponse(speakerValidation.Error);
            }

            var organizerValidation = NormalizePeople(request.Organizers, MaxOrganizers, "organizers");
            if (organizerValidation.Error is not null)
            {
                return ApiResponse<EventDto>.ErrorResponse(organizerValidation.Error);
            }

            var startDate = NormalizeUtc(request.Date);
            var endDate = NormalizeUtc(request.EndDate);
            var deadline = NormalizeUtc(request.RegistrationDeadline);
            var scheduleError = ValidateSchedule(startDate, endDate, deadline);
            if (scheduleError is not null)
            {
                return ApiResponse<EventDto>.ErrorResponse(scheduleError);
            }

            var timeZone = NormalizeTimeZone(request.TimeZone);
            if (timeZone is null)
            {
                return ApiResponse<EventDto>.ErrorResponse("The selected time zone is invalid");
            }

            var format = NormalizeFormat(request.Format);
            if (format is null)
            {
                return ApiResponse<EventDto>.ErrorResponse("Event format must be InPerson, Online, or Hybrid");
            }

            var registrationMode = NormalizeRegistrationMode(request.RegistrationMode);
            if (registrationMode is null)
            {
                return ApiResponse<EventDto>.ErrorResponse("Registration mode must be Disabled, External, or Native");
            }

            if (registrationMode == "External" && string.IsNullOrWhiteSpace(request.RegistrationUrl))
            {
                registrationMode = "Disabled";
            }

            var salesModel = NormalizeSalesModel(request.SalesModel);
            if (salesModel is null)
            {
                return ApiResponse<EventDto>.ErrorResponse("Sales model must be HCBE or Community");
            }

            if (request.TicketingEnabled)
            {
                registrationMode = "Disabled";
            }

            if (!IsValidWebUrl(request.MeetingLink) || !IsValidWebUrl(request.RegistrationUrl))
            {
                return ApiResponse<EventDto>.ErrorResponse("Meeting and registration links must use http or https");
            }

            var accessError = ValidatePublishedAccess(
                request.Status,
                format,
                request.Location,
                request.MeetingLink);
            if (accessError is not null)
            {
                return ApiResponse<EventDto>.ErrorResponse(accessError);
            }

            var eventEntity = new Event
            {
                Title = request.Title,
                TitleEn = NormalizeOptional(request.TitleEn),
                Description = request.Description,
                DescriptionEn = NormalizeOptional(request.DescriptionEn),
                Date = startDate,
                EndDate = endDate,
                TimeZone = timeZone,
                Location = request.Location,
                LocationEn = NormalizeOptional(request.LocationEn),
                Type = NormalizeOptional(request.Type),
                Format = format,
                Zone = request.Zone,
                Capacity = request.Capacity,
                RegistrationDeadline = deadline,
                MeetingLink = NormalizeOptional(request.MeetingLink),
                RegistrationUrl = NormalizeOptional(request.RegistrationUrl),
                CtaLabel = NormalizeOptional(request.CtaLabel),
                CtaLabelEn = NormalizeOptional(request.CtaLabelEn),
                RegistrationMode = registrationMode,
                AllowWaitlist = request.AllowWaitlist,
                RestrictMeetingLinkToRegistrants = request.RestrictMeetingLinkToRegistrants,
                TicketingEnabled = request.TicketingEnabled,
                SalesModel = salesModel,
                CommunityOrganizerId = request.CommunityOrganizerId,
                PlatformFeePercent = Math.Clamp(request.PlatformFeePercent, 0, 25),
                ImageUrl = request.ImageUrl,
                Status = request.Status,
                Speakers = speakerValidation.Names
                    .Select((name, index) => new EventSpeaker { Name = name, DisplayOrder = index })
                    .ToList(),
                Organizers = organizerValidation.Names
                    .Select((name, index) => new EventOrganizer { Name = name, DisplayOrder = index })
                    .ToList()
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

            return ApiResponse<EventDto>.SuccessResponse(MapToDto(eventEntity, true));
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
                .Include(e => e.Speakers)
                .Include(e => e.Organizers)
                .Include(e => e.Media)
                .Include(e => e.Attachments)
                .Include(e => e.Registrations)
                .Include(e => e.CommunityOrganizer)
                .AsSplitQuery()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return ApiResponse<EventDto>.ErrorResponse("Event not found");
            }

            var proposedStart = request.Date.HasValue ? NormalizeUtc(request.Date.Value) : eventEntity.Date;
            var proposedEnd = request.EndDate.HasValue ? NormalizeUtc(request.EndDate) : eventEntity.EndDate;
            var proposedDeadline = request.RegistrationDeadline.HasValue
                ? NormalizeUtc(request.RegistrationDeadline)
                : eventEntity.RegistrationDeadline;
            var scheduleError = ValidateSchedule(proposedStart, proposedEnd, proposedDeadline);
            if (scheduleError is not null)
            {
                return ApiResponse<EventDto>.ErrorResponse(scheduleError);
            }

            if (request.TimeZone is not null && NormalizeTimeZone(request.TimeZone) is null)
            {
                return ApiResponse<EventDto>.ErrorResponse("The selected time zone is invalid");
            }

            if (request.Format is not null && NormalizeFormat(request.Format) is null)
            {
                return ApiResponse<EventDto>.ErrorResponse("Event format must be InPerson, Online, or Hybrid");
            }


            if (request.RegistrationMode is not null && NormalizeRegistrationMode(request.RegistrationMode) is null)
            {
                return ApiResponse<EventDto>.ErrorResponse("Registration mode must be Disabled, External, or Native");
            }


            if (request.SalesModel is not null && NormalizeSalesModel(request.SalesModel) is null)
            {
                return ApiResponse<EventDto>.ErrorResponse("Sales model must be HCBE or Community");
            }

            if (!IsValidWebUrl(request.MeetingLink) || !IsValidWebUrl(request.RegistrationUrl))
            {
                return ApiResponse<EventDto>.ErrorResponse("Meeting and registration links must use http or https");
            }


            var accessDetailsChanged = request.Status is not null ||
                                       request.Format is not null ||
                                       request.Location is not null ||
                                       request.MeetingLink is not null;
            if (accessDetailsChanged)
            {
                var proposedFormat = request.Format is not null
                    ? NormalizeFormat(request.Format)!
                    : NormalizeFormat(eventEntity.Format) ?? "InPerson";
                var accessError = ValidatePublishedAccess(
                    request.Status ?? eventEntity.Status,
                    proposedFormat,
                    request.Location ?? eventEntity.Location,
                    request.MeetingLink ?? eventEntity.MeetingLink);
                if (accessError is not null)
                {
                    return ApiResponse<EventDto>.ErrorResponse(accessError);
                }
            }

            if (request.Title != null) eventEntity.Title = request.Title;
            if (request.TitleEn != null) eventEntity.TitleEn = NormalizeOptional(request.TitleEn);
            if (request.Description != null) eventEntity.Description = request.Description;
            if (request.DescriptionEn != null) eventEntity.DescriptionEn = NormalizeOptional(request.DescriptionEn);
            if (request.Date.HasValue) eventEntity.Date = proposedStart;
            if (request.EndDate.HasValue) eventEntity.EndDate = proposedEnd;
            if (request.TimeZone is not null) eventEntity.TimeZone = NormalizeTimeZone(request.TimeZone)!;
            if (request.Location != null) eventEntity.Location = request.Location;
            if (request.LocationEn != null) eventEntity.LocationEn = NormalizeOptional(request.LocationEn);
            if (request.Type != null) eventEntity.Type = NormalizeOptional(request.Type);
            if (request.Format != null) eventEntity.Format = NormalizeFormat(request.Format)!;
            if (request.Zone != null) eventEntity.Zone = request.Zone;
            if (request.Capacity.HasValue) eventEntity.Capacity = request.Capacity;
            if (request.RegistrationDeadline.HasValue) eventEntity.RegistrationDeadline = proposedDeadline;
            if (request.MeetingLink != null) eventEntity.MeetingLink = NormalizeOptional(request.MeetingLink);
            if (request.RegistrationUrl != null) eventEntity.RegistrationUrl = NormalizeOptional(request.RegistrationUrl);
            if (request.CtaLabel != null) eventEntity.CtaLabel = NormalizeOptional(request.CtaLabel);
            if (request.CtaLabelEn != null) eventEntity.CtaLabelEn = NormalizeOptional(request.CtaLabelEn);
            if (request.RegistrationMode != null) eventEntity.RegistrationMode = NormalizeRegistrationMode(request.RegistrationMode)!;
            if (request.AllowWaitlist.HasValue) eventEntity.AllowWaitlist = request.AllowWaitlist.Value;
            if (request.RestrictMeetingLinkToRegistrants.HasValue) eventEntity.RestrictMeetingLinkToRegistrants = request.RestrictMeetingLinkToRegistrants.Value;
            if (request.TicketingEnabled.HasValue)
            {
                eventEntity.TicketingEnabled = request.TicketingEnabled.Value;
                if (request.TicketingEnabled.Value) eventEntity.RegistrationMode = "Disabled";
            }
            if (request.SalesModel is not null) eventEntity.SalesModel = NormalizeSalesModel(request.SalesModel)!;
            if (request.ClearCommunityOrganizer) eventEntity.CommunityOrganizerId = null;
            else if (request.CommunityOrganizerId.HasValue) eventEntity.CommunityOrganizerId = request.CommunityOrganizerId;
            if (request.PlatformFeePercent.HasValue) eventEntity.PlatformFeePercent = Math.Clamp(request.PlatformFeePercent.Value, 0, 25);
            if (request.ImageUrl != null) eventEntity.ImageUrl = request.ImageUrl;
            if (request.Status != null) eventEntity.Status = request.Status;

            if (request.Speakers is not null)
            {
                var speakerValidation = NormalizeSpeakers(request.Speakers);
                if (speakerValidation.Error is not null)
                {
                    return ApiResponse<EventDto>.ErrorResponse(speakerValidation.Error);
                }

                var existingSpeakers = eventEntity.Speakers
                    .OrderBy(speaker => speaker.DisplayOrder)
                    .ToList();
                var sharedCount = Math.Min(existingSpeakers.Count, speakerValidation.Names.Count);

                for (var index = 0; index < sharedCount; index++)
                {
                    existingSpeakers[index].Name = speakerValidation.Names[index];
                    existingSpeakers[index].DisplayOrder = index;
                }

                if (existingSpeakers.Count > speakerValidation.Names.Count)
                {
                    _context.EventSpeakers.RemoveRange(existingSpeakers.Skip(speakerValidation.Names.Count));
                }

                for (var index = existingSpeakers.Count; index < speakerValidation.Names.Count; index++)
                {
                    var newSpeaker = new EventSpeaker
                    {
                        EventId = eventEntity.Id,
                        Name = speakerValidation.Names[index],
                        DisplayOrder = index
                    };
                    eventEntity.Speakers.Add(newSpeaker);
                    _context.EventSpeakers.Add(newSpeaker);
                }
            }

            if (request.Organizers is not null)
            {
                var organizerValidation = NormalizePeople(request.Organizers, MaxOrganizers, "organizers");
                if (organizerValidation.Error is not null)
                {
                    return ApiResponse<EventDto>.ErrorResponse(organizerValidation.Error);
                }

                var existingOrganizers = eventEntity.Organizers
                    .OrderBy(organizer => organizer.DisplayOrder)
                    .ToList();
                var sharedCount = Math.Min(existingOrganizers.Count, organizerValidation.Names.Count);

                for (var index = 0; index < sharedCount; index++)
                {
                    existingOrganizers[index].Name = organizerValidation.Names[index];
                    existingOrganizers[index].DisplayOrder = index;
                }

                if (existingOrganizers.Count > organizerValidation.Names.Count)
                {
                    _context.EventOrganizers.RemoveRange(existingOrganizers.Skip(organizerValidation.Names.Count));
                }

                for (var index = existingOrganizers.Count; index < organizerValidation.Names.Count; index++)
                {
                    var organizer = new EventOrganizer
                    {
                        EventId = eventEntity.Id,
                        Name = organizerValidation.Names[index],
                        DisplayOrder = index
                    };
                    eventEntity.Organizers.Add(organizer);
                    _context.EventOrganizers.Add(organizer);
                }
            }

            eventEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<EventDto>.SuccessResponse(MapToDto(eventEntity, true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update event {EventId}", id);
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

    private static (List<string> Names, string? Error) NormalizeSpeakers(IEnumerable<string>? speakers)
    {
        return NormalizePeople(speakers, MaxSpeakers, "speakers");
    }

    private static (List<string> Names, string? Error) NormalizePeople(
        IEnumerable<string>? values,
        int maximum,
        string label)
    {
        var names = (values ?? Enumerable.Empty<string>())
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count > maximum)
        {
            return (names, $"An event can have at most {maximum} {label}.");
        }

        if (names.Any(name => name.Length > MaxSpeakerNameLength))
        {
            return (names, $"Names cannot exceed {MaxSpeakerNameLength} characters.");
        }

        return (names, null);
    }

    private static string? ValidateSchedule(DateTime start, DateTime? end, DateTime? deadline)
    {
        if (end.HasValue && end.Value <= start)
        {
            return "Event end time must be after its start time";
        }

        if (deadline.HasValue && deadline.Value >= start)
        {
            return "Registration deadline must be before the event start time";
        }

        return null;
    }

    private static string? NormalizeTimeZone(string? value)
    {
        var timeZone = NormalizeOptional(value) ?? DefaultTimeZone;
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return timeZone;
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    private static string? NormalizeFormat(string? value)
    {
        var format = NormalizeOptional(value) ?? "InPerson";
        return AllowedFormats.FirstOrDefault(item => item.Equals(format, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeRegistrationMode(string? value)
    {
        var mode = NormalizeOptional(value) ?? "External";
        return AllowedRegistrationModes.FirstOrDefault(item => item.Equals(mode, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsValidWebUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string? ValidatePublishedAccess(
        string status,
        string format,
        string? location,
        string? meetingLink)
    {
        if (!IsPublishedStatus(status)) return null;

        if (!format.Equals("Online", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(location))
        {
            return "A location is required for published in-person and hybrid events";
        }

        if (!format.Equals("InPerson", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(meetingLink))
        {
            return "A meeting link is required for published online and hybrid events";
        }

        return null;
    }

    private static bool IsPublishedStatus(string status)
    {
        var normalized = status.Trim().ToLowerInvariant();
        return normalized is "active" or "published" or "publie" or "publié" or "a venir" or "à venir";
    }

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

    private static EventDto MapToDto(Event eventEntity, bool includeRestrictedAccess = false)
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

        var speakers = (eventEntity.Speakers ?? Enumerable.Empty<EventSpeaker>())
            .OrderBy(s => s.DisplayOrder)
            .Select(s => s.Name)
            .ToList();

        var organizers = (eventEntity.Organizers ?? Enumerable.Empty<EventOrganizer>())
            .OrderBy(organizer => organizer.DisplayOrder)
            .Select(organizer => organizer.Name)
            .ToList();

        var confirmedCount = (eventEntity.Registrations ?? Enumerable.Empty<EventRegistration>())
            .Count(registration => registration.Status is "Confirmed" or "Attended");
        var waitlistCount = (eventEntity.Registrations ?? Enumerable.Empty<EventRegistration>())
            .Count(registration => registration.Status == "Waitlisted");
        int? remainingCapacity = eventEntity.Capacity.HasValue
            ? Math.Max(0, eventEntity.Capacity.Value - confirmedCount)
            : null;
        var meetingLink = eventEntity.RestrictMeetingLinkToRegistrants && !includeRestrictedAccess
            ? null
            : eventEntity.MeetingLink;

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
            meetingLink,
            eventEntity.ImageUrl,
            eventEntity.Status,
            eventEntity.CreatedAt,
            eventEntity.UpdatedAt,
            eventEntity.TitleEn,
            eventEntity.DescriptionEn,
            eventEntity.LocationEn,
            speakers,
            media,
            attachments,
            eventEntity.EndDate,
            eventEntity.TimeZone,
            eventEntity.Format,
            eventEntity.RegistrationUrl,
            eventEntity.CtaLabel,
            eventEntity.CtaLabelEn,
            organizers,
            eventEntity.RegistrationMode,
            eventEntity.AllowWaitlist,
            eventEntity.RestrictMeetingLinkToRegistrants,
            confirmedCount,
            waitlistCount,
            remainingCapacity,
            eventEntity.TicketingEnabled,
            eventEntity.SalesModel,
            eventEntity.CommunityOrganizerId,
            eventEntity.CommunityOrganizer?.DisplayName,
            eventEntity.PlatformFeePercent
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

    private static string? NormalizeSalesModel(string? value)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? "HCBE" : value.Trim();
        return AllowedSalesModels.FirstOrDefault(item => item.Equals(candidate, StringComparison.OrdinalIgnoreCase));
    }
}
