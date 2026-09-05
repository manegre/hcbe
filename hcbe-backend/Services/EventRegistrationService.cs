using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class EventRegistrationService(
    ApplicationDbContext context,
    IEmailOutbox emailOutbox,
    IEmailTemplateRenderer emailTemplates,
    IConfiguration configuration,
    INotificationService notifications) : IEventRegistrationService
{
    private static readonly HashSet<string> AdminStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Confirmed", "Waitlisted", "Cancelled", "Attended", "NoShow"
    };

    public async Task<ApiResponse<EventRegistrationDto>> RegisterAsync(
        Guid userId,
        Guid eventId,
        CreateEventRegistrationRequest request)
    {
        var member = await FindMemberAsync(userId);
        if (member is null)
        {
            return ApiResponse<EventRegistrationDto>.ErrorResponse("A completed member account is required to register");
        }

        var eventEntity = await context.Events
            .Include(item => item.Registrations)
            .FirstOrDefaultAsync(item => item.Id == eventId);
        if (eventEntity is null || !IsPublic(eventEntity))
        {
            return ApiResponse<EventRegistrationDto>.ErrorResponse("Event not found");
        }

        if (!eventEntity.RegistrationMode.Equals("Native", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<EventRegistrationDto>.ErrorResponse("This event does not accept registrations on HCBE Canada");
        }

        var now = DateTime.UtcNow;
        if (eventEntity.Date <= now)
        {
            return ApiResponse<EventRegistrationDto>.ErrorResponse("Registration is closed because this event has started");
        }

        if (eventEntity.RegistrationDeadline.HasValue && eventEntity.RegistrationDeadline.Value <= now)
        {
            return ApiResponse<EventRegistrationDto>.ErrorResponse("The registration deadline has passed");
        }

        var existing = eventEntity.Registrations.FirstOrDefault(item => item.MemberId == member.Id);
        if (existing is not null && existing.Status != "Cancelled")
        {
            return ApiResponse<EventRegistrationDto>.SuccessResponse(Map(existing, eventEntity));
        }

        var confirmedCount = eventEntity.Registrations.Count(item => item.Status is "Confirmed" or "Attended");
        var isFull = eventEntity.Capacity.HasValue && confirmedCount >= eventEntity.Capacity.Value;
        if (isFull && !eventEntity.AllowWaitlist)
        {
            return ApiResponse<EventRegistrationDto>.ErrorResponse("This event is full and the waiting list is closed");
        }

        var status = isFull ? "Waitlisted" : "Confirmed";
        var registration = existing ?? new EventRegistration
        {
            EventId = eventEntity.Id,
            MemberId = member.Id,
            ConfirmationCode = CreateConfirmationCode(),
            RegisteredAt = now
        };

        registration.Event = eventEntity;
        registration.Member = member;
        registration.Status = status;
        registration.AccessibilityNeeds = Normalize(request.AccessibilityNeeds);
        registration.CancelledAt = null;
        registration.CheckedInAt = null;
        registration.UpdatedAt = now;
        if (existing is null) context.EventRegistrations.Add(registration);

        await context.SaveChangesAsync();
        QueueStatusEmail(registration, eventEntity, member, status);
        await QueueStatusNotificationAsync(userId, registration, eventEntity, status);
        await context.SaveChangesAsync();

        var waitlistPosition = await WaitlistPositionAsync(registration);
        return ApiResponse<EventRegistrationDto>.SuccessResponse(Map(registration, eventEntity, waitlistPosition));
    }

    public async Task<ApiResponse<EventRegistrationDto>> GetMineForEventAsync(Guid userId, Guid eventId)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<EventRegistrationDto>.ErrorResponse("Member account not found");

        var registration = await Query()
            .FirstOrDefaultAsync(item => item.EventId == eventId && item.MemberId == memberId);
        if (registration is null) return ApiResponse<EventRegistrationDto>.ErrorResponse("Registration not found");
        var position = await WaitlistPositionAsync(registration);
        return ApiResponse<EventRegistrationDto>.SuccessResponse(Map(registration, registration.Event!, position));
    }

    public async Task<ApiResponse<List<EventRegistrationDto>>> GetMineAsync(Guid userId)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<EventRegistrationDto>>.ErrorResponse("Member account not found");

        var registrations = await Query()
            .Where(item => item.MemberId == memberId)
            .OrderByDescending(item => item.Event!.Date)
            .ToListAsync();
        var positions = WaitlistPositions(registrations);
        return ApiResponse<List<EventRegistrationDto>>.SuccessResponse(
            registrations.Select(item => Map(item, item.Event!, positions.GetValueOrDefault(item.Id))).ToList());
    }

    public async Task<ApiResponse<EventRegistrationDto>> CancelAsync(Guid userId, Guid eventId)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<EventRegistrationDto>.ErrorResponse("Member account not found");

        var registration = await Query(tracking: true)
            .FirstOrDefaultAsync(item => item.EventId == eventId && item.MemberId == memberId);
        if (registration is null) return ApiResponse<EventRegistrationDto>.ErrorResponse("Registration not found");
        if (registration.Status == "Cancelled") return ApiResponse<EventRegistrationDto>.SuccessResponse(Map(registration, registration.Event!));
        if (registration.Event!.Date <= DateTime.UtcNow) return ApiResponse<EventRegistrationDto>.ErrorResponse("A past registration cannot be cancelled");

        registration.Status = "Cancelled";
        registration.CancelledAt = DateTime.UtcNow;
        registration.UpdatedAt = DateTime.UtcNow;
        registration.CheckedInAt = null;
        await context.SaveChangesAsync();
        var promoted = await PromoteWaitlistedAsync(registration.Event);
        await context.SaveChangesAsync();

        QueueStatusEmail(registration, registration.Event, registration.Member!, "Cancelled");
        await QueueStatusNotificationAsync(userId, registration, registration.Event, "Cancelled");
        if (promoted is not null)
        {
            QueueStatusEmail(promoted, registration.Event, promoted.Member!, "Confirmed");
            await QueueStatusNotificationForMemberAsync(promoted.MemberId, promoted, registration.Event, "Confirmed");
        }
        await context.SaveChangesAsync();
        return ApiResponse<EventRegistrationDto>.SuccessResponse(Map(registration, registration.Event));
    }

    public async Task<ApiResponse<List<EventRegistrationDto>>> GetForAdminAsync(Guid eventId, string? status, string? search)
    {
        var query = Query().Where(item => item.EventId == eventId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item =>
                (item.Member!.FirstName + " " + item.Member.LastName).ToLower().Contains(term) ||
                item.Member.Email.ToLower().Contains(term) ||
                item.ConfirmationCode.ToLower().Contains(term));
        }

        var registrations = await query
            .OrderBy(item => item.Status == "Waitlisted" ? 1 : 0)
            .ThenBy(item => item.RegisteredAt)
            .ToListAsync();
        var positions = WaitlistPositions(registrations);
        return ApiResponse<List<EventRegistrationDto>>.SuccessResponse(
            registrations.Select(item => Map(item, item.Event!, positions.GetValueOrDefault(item.Id))).ToList());
    }

    public async Task<ApiResponse<EventRegistrationDto>> UpdateForAdminAsync(
        Guid eventId,
        Guid registrationId,
        UpdateEventRegistrationRequest request)
    {
        var normalizedStatus = AdminStatuses.FirstOrDefault(item => item.Equals(request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (normalizedStatus is null)
        {
            return ApiResponse<EventRegistrationDto>.ErrorResponse("Unsupported registration status");
        }

        var registration = await Query(tracking: true)
            .FirstOrDefaultAsync(item => item.Id == registrationId && item.EventId == eventId);
        if (registration is null) return ApiResponse<EventRegistrationDto>.ErrorResponse("Registration not found");

        var previousStatus = registration.Status;
        if (normalizedStatus == "Confirmed" && previousStatus is not "Confirmed" and not "Attended")
        {
            var confirmedCount = await context.EventRegistrations.CountAsync(item =>
                item.EventId == eventId && item.Id != registrationId &&
                (item.Status == "Confirmed" || item.Status == "Attended"));
            if (registration.Event!.Capacity.HasValue && confirmedCount >= registration.Event.Capacity.Value)
            {
                return ApiResponse<EventRegistrationDto>.ErrorResponse("The event is already at capacity");
            }
        }

        registration.Status = normalizedStatus;
        registration.AdminNotes = Normalize(request.AdminNotes);
        registration.UpdatedAt = DateTime.UtcNow;
        registration.CancelledAt = normalizedStatus == "Cancelled" ? DateTime.UtcNow : null;
        registration.CheckedInAt = normalizedStatus == "Attended" ? registration.CheckedInAt ?? DateTime.UtcNow : null;

        EventRegistration? promoted = null;
        if (previousStatus is "Confirmed" or "Attended" && normalizedStatus is "Cancelled" or "NoShow")
        {
            await context.SaveChangesAsync();
            promoted = await PromoteWaitlistedAsync(registration.Event!);
        }

        await context.SaveChangesAsync();
        if (previousStatus != normalizedStatus)
        {
            QueueStatusEmail(registration, registration.Event!, registration.Member!, normalizedStatus);
            await QueueStatusNotificationForMemberAsync(registration.MemberId, registration, registration.Event!, normalizedStatus);
        }
        if (promoted is not null)
        {
            QueueStatusEmail(promoted, registration.Event!, promoted.Member!, "Confirmed");
            await QueueStatusNotificationForMemberAsync(promoted.MemberId, promoted, registration.Event!, "Confirmed");
        }
        await context.SaveChangesAsync();
        var position = await WaitlistPositionAsync(registration);
        return ApiResponse<EventRegistrationDto>.SuccessResponse(Map(registration, registration.Event!, position));
    }

    public async Task<ApiResponse<EventRegistrationDto>> CheckInByCodeAsync(Guid eventId, string confirmationCode)
    {
        var code = confirmationCode.Trim().ToUpperInvariant();
        if (code.Length == 0) return ApiResponse<EventRegistrationDto>.ErrorResponse("Confirmation code is required");
        var registration = await Query(tracking: true).FirstOrDefaultAsync(item => item.EventId == eventId && item.ConfirmationCode == code);
        if (registration is null) return ApiResponse<EventRegistrationDto>.ErrorResponse("Registration not found");
        if (registration.Status is "Cancelled" or "Waitlisted") return ApiResponse<EventRegistrationDto>.ErrorResponse("This registration cannot be checked in");
        registration.Status = "Attended";
        registration.CheckedInAt ??= DateTime.UtcNow;
        registration.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return ApiResponse<EventRegistrationDto>.SuccessResponse(Map(registration, registration.Event!));
    }

    public async Task<(byte[]? Content, string? FileName)> BuildCalendarAsync(Guid eventId)
    {
        var item = await context.Events.AsNoTracking().FirstOrDefaultAsync(eventEntity =>
            eventEntity.Id == eventId &&
            eventEntity.Status != "Draft" && eventEntity.Status != "Cancelled" &&
            eventEntity.Status != "Brouillon" && eventEntity.Status != "Annulé");
        if (item is null) return (null, null);

        static string Escape(string? value) => (value ?? string.Empty)
            .Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
            .Replace("\r\n", "\\n").Replace("\n", "\\n");
        static string Utc(DateTime value) => value.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        var eventUrl = $"{PublicAppUrl()}/actualites/evenements/{item.Id}";
        var builder = new StringBuilder()
            .AppendLine("BEGIN:VCALENDAR")
            .AppendLine("VERSION:2.0")
            .AppendLine("PRODID:-//HCBE Canada//Events//FR")
            .AppendLine("CALSCALE:GREGORIAN")
            .AppendLine("BEGIN:VEVENT")
            .AppendLine($"UID:{item.Id}@hcbe.ca")
            .AppendLine($"DTSTAMP:{Utc(DateTime.UtcNow)}")
            .AppendLine($"DTSTART:{Utc(item.Date)}")
            .AppendLine($"DTEND:{Utc(item.EndDate ?? item.Date.AddHours(1))}")
            .AppendLine($"SUMMARY:{Escape(item.Title)}")
            .AppendLine($"DESCRIPTION:{Escape(item.Description)}")
            .AppendLine($"LOCATION:{Escape(item.Location)}")
            .AppendLine($"URL:{eventUrl}")
            .AppendLine("END:VEVENT")
            .AppendLine("END:VCALENDAR");
        return (Encoding.UTF8.GetBytes(builder.ToString()), $"hcbe-{Slug(item.Title)}.ics");
    }

    public async Task<ApiResponse<EventAttendanceStatsDto>> GetStatsAsync(Guid eventId)
    {
        if (!await context.Events.AnyAsync(item => item.Id == eventId))
            return ApiResponse<EventAttendanceStatsDto>.ErrorResponse("Event not found");
        var statuses = await context.EventRegistrations.AsNoTracking()
            .Where(item => item.EventId == eventId)
            .GroupBy(item => item.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count);
        var ratings = await context.EventSurveyResponses.AsNoTracking()
            .Where(item => item.EventRegistration.EventId == eventId)
            .Select(item => item.Rating).ToListAsync();
        int Count(string status) => statuses.GetValueOrDefault(status);
        var eligible = Count("Attended") + Count("NoShow");
        var rate = eligible == 0 ? 0 : Math.Round(Count("Attended") * 100d / eligible, 1);
        return ApiResponse<EventAttendanceStatsDto>.SuccessResponse(new(
            statuses.Values.Sum(), Count("Confirmed"), Count("Waitlisted"), Count("Attended"),
            Count("NoShow"), Count("Cancelled"), rate,
            ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 1), ratings.Count));
    }

    public async Task<ApiResponse<EventSurveyResponseDto>> SubmitSurveyAsync(Guid userId, Guid eventId, SubmitEventSurveyRequest request)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<EventSurveyResponseDto>.ErrorResponse("Member account not found");
        var registration = await context.EventRegistrations.Include(item => item.Event).Include(item => item.SurveyResponse)
            .FirstOrDefaultAsync(item => item.EventId == eventId && item.MemberId == memberId);
        if (registration is null || registration.Status != "Attended")
            return ApiResponse<EventSurveyResponseDto>.ErrorResponse("Attendance is required before submitting the survey");
        if (registration.Event!.Date > DateTime.UtcNow)
            return ApiResponse<EventSurveyResponseDto>.ErrorResponse("The survey opens after the event starts");
        var response = registration.SurveyResponse ?? new EventSurveyResponse { EventRegistrationId = registration.Id };
        response.Rating = request.Rating;
        response.Feedback = Normalize(request.Feedback);
        response.ConsentToQuote = request.ConsentToQuote;
        response.UpdatedAtUtc = DateTime.UtcNow;
        if (registration.SurveyResponse is null) context.EventSurveyResponses.Add(response);
        await context.SaveChangesAsync();
        return ApiResponse<EventSurveyResponseDto>.SuccessResponse(MapSurvey(response));
    }

    public async Task<ApiResponse<EventSurveyResponseDto>> GetMySurveyAsync(Guid userId, Guid eventId)
    {
        var memberId = await GetMemberIdAsync(userId);
        var response = memberId is null ? null : await context.EventSurveyResponses.AsNoTracking()
            .FirstOrDefaultAsync(item => item.EventRegistration.EventId == eventId && item.EventRegistration.MemberId == memberId);
        return response is null
            ? ApiResponse<EventSurveyResponseDto>.ErrorResponse("Survey response not found")
            : ApiResponse<EventSurveyResponseDto>.SuccessResponse(MapSurvey(response));
    }

    public async Task<(byte[]? Content, string? FileName)> BuildCertificateAsync(Guid userId, Guid eventId)
    {
        var memberId = await GetMemberIdAsync(userId);
        var registration = memberId is null ? null : await Query().FirstOrDefaultAsync(item =>
            item.EventId == eventId && item.MemberId == memberId && item.Status == "Attended");
        if (registration is null) return (null, null);
        return (ReceiptPdfRenderer.RenderEventCertificate(registration), $"HCBE-attestation-{registration.ConfirmationCode}.pdf");
    }

    public async Task<ApiResponse<EventCommunicationDto>> SendCommunicationAsync(Guid userId, Guid eventId, SendEventCommunicationRequest request)
    {
        var allowed = new[] { "Active", "Confirmed", "Waitlisted", "Attended", "NoShow", "Cancelled" };
        var audience = allowed.FirstOrDefault(item => item.Equals(request.Audience.Trim(), StringComparison.OrdinalIgnoreCase));
        if (audience is null) return ApiResponse<EventCommunicationDto>.ErrorResponse("Unsupported audience");
        var eventEntity = await context.Events.FirstOrDefaultAsync(item => item.Id == eventId);
        if (eventEntity is null) return ApiResponse<EventCommunicationDto>.ErrorResponse("Event not found");
        var query = context.EventRegistrations.Include(item => item.Member).Where(item => item.EventId == eventId);
        query = audience == "Active"
            ? query.Where(item => item.Status == "Confirmed" || item.Status == "Waitlisted" || item.Status == "Attended")
            : query.Where(item => item.Status == audience);
        var recipients = await query.ToListAsync();
        var communication = new EventCommunication
        {
            EventId = eventId, SentByUserId = userId, Audience = audience,
            Subject = request.Subject.Trim(), Body = request.Body.Trim(), RecipientCount = recipients.Count
        };
        context.EventCommunications.Add(communication);
        var eventUrl = $"{PublicAppUrl()}/actualites/evenements/{eventId}";
        foreach (var recipient in recipients.Where(item => item.Member is not null))
        {
            var email = emailTemplates.EventMessage(recipient.Member!.FirstName, eventEntity.Title, communication.Subject, communication.Body, eventUrl);
            emailOutbox.Enqueue(recipient.Member.Email, email.Subject, email.HtmlBody, nameof(EventCommunication), communication.Id);
        }
        await context.SaveChangesAsync();
        return ApiResponse<EventCommunicationDto>.SuccessResponse(MapCommunication(communication));
    }

    public async Task<ApiResponse<List<EventCommunicationDto>>> GetCommunicationsAsync(Guid eventId)
    {
        var items = await context.EventCommunications.AsNoTracking().Where(item => item.EventId == eventId)
            .OrderByDescending(item => item.SentAtUtc).Take(20).ToListAsync();
        return ApiResponse<List<EventCommunicationDto>>.SuccessResponse(items.Select(MapCommunication).ToList());
    }

    private static EventSurveyResponseDto MapSurvey(EventSurveyResponse item) => new(
        item.Id, item.EventRegistrationId, item.Rating, item.Feedback, item.ConsentToQuote, item.SubmittedAtUtc, item.UpdatedAtUtc);
    private static EventCommunicationDto MapCommunication(EventCommunication item) => new(
        item.Id, item.Audience, item.Subject, item.Body, item.RecipientCount, item.SentAtUtc);

    private IQueryable<EventRegistration> Query(bool tracking = false)
    {
        var query = context.EventRegistrations
            .Include(item => item.Event)
            .Include(item => item.Member)
            .AsQueryable();
        return tracking ? query : query.AsNoTracking();
    }

    private async Task<EventRegistration?> PromoteWaitlistedAsync(Event eventEntity)
    {
        var confirmedCount = await context.EventRegistrations.CountAsync(item =>
            item.EventId == eventEntity.Id && (item.Status == "Confirmed" || item.Status == "Attended"));
        if (eventEntity.Capacity.HasValue && confirmedCount >= eventEntity.Capacity.Value) return null;

        var next = await context.EventRegistrations
            .Include(item => item.Member)
            .Where(item => item.EventId == eventEntity.Id && item.Status == "Waitlisted")
            .OrderBy(item => item.RegisteredAt)
            .FirstOrDefaultAsync();
        if (next is null) return null;
        next.Status = "Confirmed";
        next.UpdatedAt = DateTime.UtcNow;
        return next;
    }

    private void QueueStatusEmail(EventRegistration registration, Event eventEntity, Member member, string status)
    {
        var eventUrl = $"{PublicAppUrl()}/actualites/evenements/{eventEntity.Id}";
        var email = emailTemplates.EventRegistrationUpdate(
            member.FirstName,
            eventEntity.Title,
            eventEntity.Date,
            status,
            registration.ConfirmationCode,
            eventUrl);
        emailOutbox.Enqueue(member.Email, email.Subject, email.HtmlBody, nameof(EventRegistration), registration.Id);
    }

    private Task QueueStatusNotificationAsync(Guid userId, EventRegistration registration, Event eventEntity, string status)
    {
        var label = status switch
        {
            "Confirmed" => "Inscription confirmée / Registration confirmed",
            "Waitlisted" => "Liste d’attente / Waiting list",
            "Cancelled" => "Inscription annulée / Registration cancelled",
            "Attended" => "Présence confirmée / Attendance confirmed",
            _ => "Inscription mise à jour / Registration updated"
        };
        return notifications.CreateForUserAsync(userId, "event-registration", label,
            $"{eventEntity.Title} — {registration.ConfirmationCode}", eventEntity.Id,
            $"/actualites/evenements/{eventEntity.Id}");
    }

    private async Task QueueStatusNotificationForMemberAsync(Guid memberId, EventRegistration registration, Event eventEntity, string status)
    {
        var userId = await context.Users.AsNoTracking().Where(user => user.MemberId == memberId && user.IsActive)
            .Select(user => (Guid?)user.Id).FirstOrDefaultAsync();
        if (userId.HasValue) await QueueStatusNotificationAsync(userId.Value, registration, eventEntity, status);
    }

    private EventRegistrationDto Map(EventRegistration item, Event eventEntity, int? knownWaitlistPosition = null)
    {
        int? waitlistPosition = item.Status == "Waitlisted" ? knownWaitlistPosition : null;
        var canAccessMeeting = item.Status is "Confirmed" or "Attended";
        return new EventRegistrationDto(
            item.Id,
            item.EventId,
            eventEntity.Title,
            item.MemberId,
            $"{item.Member?.FirstName} {item.Member?.LastName}".Trim(),
            item.Member?.Email ?? string.Empty,
            item.Status,
            item.ConfirmationCode,
            item.AccessibilityNeeds,
            item.AdminNotes,
            waitlistPosition,
            item.RegisteredAt,
            item.UpdatedAt,
            item.CancelledAt,
            item.CheckedInAt,
            canAccessMeeting ? eventEntity.MeetingLink : null);
    }

    private Task<Member?> FindMemberAsync(Guid userId) => context.Users
        .Where(user => user.Id == userId && user.IsActive && user.MemberId != null)
        .Select(user => user.Member)
        .FirstOrDefaultAsync();

    private Task<Guid?> GetMemberIdAsync(Guid userId) => context.Users
        .AsNoTracking()
        .Where(user => user.Id == userId && user.IsActive)
        .Select(user => user.MemberId)
        .FirstOrDefaultAsync();

    private async Task<int?> WaitlistPositionAsync(EventRegistration item) => item.Status == "Waitlisted"
        ? await context.EventRegistrations.CountAsync(candidate =>
            candidate.EventId == item.EventId && candidate.Status == "Waitlisted" &&
            candidate.RegisteredAt <= item.RegisteredAt)
        : null;

    private static Dictionary<Guid, int?> WaitlistPositions(IEnumerable<EventRegistration> registrations) => registrations
        .Where(item => item.Status == "Waitlisted")
        .GroupBy(item => item.EventId)
        .SelectMany(group => group.OrderBy(item => item.RegisteredAt).ThenBy(item => item.Id)
            .Select((item, index) => new { item.Id, Position = (int?)index + 1 }))
        .ToDictionary(item => item.Id, item => item.Position);

    private string PublicAppUrl() => (configuration["PublicAppUrl"] ?? "http://localhost:3000").TrimEnd('/');
    private static bool IsPublic(Event item) => item.Status is not "Draft" and not "Cancelled" and not "Brouillon" and not "Annulé";
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string CreateConfirmationCode() => Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
    private static string Slug(string value) => string.Concat(value.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-')).Trim('-');
}
