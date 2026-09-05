using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class MemberEngagementService(
    ApplicationDbContext context,
    IEmailOutbox emailOutbox,
    IEmailTemplateRenderer emailTemplates,
    IConfiguration configuration) : IMemberEngagementService
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase) { "Event", "Opportunity" };

    public async Task<ApiResponse<MemberEngagementDashboardDto>> GetDashboardAsync(Guid userId)
    {
        var user = await context.Users.AsNoTracking().Include(item => item.Member)
            .SingleOrDefaultAsync(item => item.Id == userId && item.IsActive);
        if (user?.Member is null) return ApiResponse<MemberEngagementDashboardDto>.ErrorResponse("Member account not found");

        var now = DateTime.UtcNow;
        var events = await context.EventRegistrations.AsNoTracking()
            .Include(item => item.Event)
            .Where(item => item.MemberId == user.MemberId && item.Event!.Date >= now && item.Status != "Cancelled")
            .OrderBy(item => item.Event!.Date).Take(4)
            .Select(item => new MemberDashboardEventDto(item.EventId, item.Event!.Title, item.Event.TitleEn,
                item.Event.Date, item.Event.Location, item.Status, item.ConfirmationCode))
            .ToListAsync();
        var appliedIds = context.OpportunityApplications.AsNoTracking().Where(item => item.MemberId == user.MemberId).Select(item => item.OpportunityId);
        var opportunities = await context.Opportunities.AsNoTracking()
            .Where(item => item.Status == "Published" && (!item.DeadlineUtc.HasValue || item.DeadlineUtc >= now) && !appliedIds.Contains(item.Id))
            .OrderBy(item => item.DeadlineUtc ?? DateTime.MaxValue).Take(4)
            .Select(item => new MemberDashboardOpportunityDto(item.Id, item.Title, item.TitleEn, item.Type,
                item.Organization, item.Location, item.IsRemote, item.DeadlineUtc)).ToListAsync();
        var saved = await ResolveSavedAsync(userId);
        var notifications = await context.Notifications.AsNoTracking().Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt).Take(5)
            .Select(item => new NotificationDto(item.Id, item.Type, item.Title, item.Message, item.RelatedEntityId,
                item.Link, item.IsRead, item.UserId, item.CreatedAt, item.ReadAt)).ToListAsync();
        var unreadNotifications = await context.Notifications.CountAsync(item => item.UserId == userId && !item.IsRead);
        var unreadMessages = await context.PrivateMessages.AsNoTracking()
            .Where(message => message.ReadAt == null && message.SenderMemberId != user.MemberId &&
                (message.Conversation!.MemberOneId == user.MemberId || message.Conversation.MemberTwoId == user.MemberId))
            .CountAsync();
        var openCases = await context.ServiceCases.AsNoTracking()
            .CountAsync(item => item.MemberId == user.MemberId && item.Status != "Resolved" && item.Status != "Closed");
        var standing = await context.MembershipStandings.AsNoTracking().Where(item => item.UserId == userId)
            .Select(item => item.Status).FirstOrDefaultAsync() ?? MembershipStatuses.Active;
        var name = $"{user.Member.FirstName} {user.Member.LastName}".Trim();
        return ApiResponse<MemberEngagementDashboardDto>.SuccessResponse(new(name, standing, unreadNotifications,
            unreadMessages, openCases, events, opportunities, saved, notifications));
    }

    public async Task<ApiResponse<List<SavedMemberItemDto>>> GetSavedAsync(Guid userId) =>
        ApiResponse<List<SavedMemberItemDto>>.SuccessResponse(await ResolveSavedAsync(userId));

    public async Task<ApiResponse<SavedMemberItemDto>> SaveAsync(Guid userId, string entityType, Guid entityId)
    {
        var normalized = NormalizeType(entityType);
        if (normalized is null) return ApiResponse<SavedMemberItemDto>.ErrorResponse("Entity type must be Event or Opportunity");
        if (!await context.Users.AnyAsync(item => item.Id == userId && item.IsActive && item.MemberId != null))
            return ApiResponse<SavedMemberItemDto>.ErrorResponse("Member account not found");
        if (!await IsPublishedAsync(normalized, entityId)) return ApiResponse<SavedMemberItemDto>.ErrorResponse("Item not found");
        var existing = await context.SavedMemberItems.FirstOrDefaultAsync(item => item.UserId == userId && item.EntityType == normalized && item.EntityId == entityId);
        if (existing is null)
        {
            existing = new SavedMemberItem { UserId = userId, EntityType = normalized, EntityId = entityId };
            context.SavedMemberItems.Add(existing);
            await context.SaveChangesAsync();
        }
        var dto = (await ResolveSavedAsync(userId)).Single(item => item.EntityType == normalized && item.EntityId == entityId);
        return ApiResponse<SavedMemberItemDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse> RemoveSavedAsync(Guid userId, string entityType, Guid entityId)
    {
        var normalized = NormalizeType(entityType);
        if (normalized is null) return ApiResponse.CreateError("Entity type must be Event or Opportunity");
        var item = await context.SavedMemberItems.FirstOrDefaultAsync(candidate => candidate.UserId == userId && candidate.EntityType == normalized && candidate.EntityId == entityId);
        if (item is not null) { context.SavedMemberItems.Remove(item); await context.SaveChangesAsync(); }
        return ApiResponse.CreateSuccess("Saved item removed");
    }

    public async Task<ApiResponse<List<MemberBlockDto>>> GetBlocksAsync(Guid userId)
    {
        var memberId = await MemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<MemberBlockDto>>.ErrorResponse("Member account not found");
        var items = await context.MemberBlocks.AsNoTracking().Include(item => item.BlockedMember)
            .Where(item => item.BlockerMemberId == memberId).OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new MemberBlockDto(item.Id, item.BlockedMemberId,
                (item.BlockedMember!.FirstName + " " + item.BlockedMember.LastName).Trim(), item.CreatedAtUtc)).ToListAsync();
        return ApiResponse<List<MemberBlockDto>>.SuccessResponse(items);
    }

    public async Task<ApiResponse<MemberBlockDto>> BlockAsync(Guid userId, Guid blockedMemberId)
    {
        var memberId = await MemberIdAsync(userId);
        if (memberId is null) return ApiResponse<MemberBlockDto>.ErrorResponse("Member account not found");
        if (memberId == blockedMemberId) return ApiResponse<MemberBlockDto>.ErrorResponse("You cannot block yourself");
        var blocked = await context.Members.FindAsync(blockedMemberId);
        if (blocked is null) return ApiResponse<MemberBlockDto>.ErrorResponse("Member not found");
        var item = await context.MemberBlocks.FirstOrDefaultAsync(candidate => candidate.BlockerMemberId == memberId && candidate.BlockedMemberId == blockedMemberId);
        if (item is null)
        {
            item = new MemberBlock { BlockerMemberId = memberId.Value, BlockedMemberId = blockedMemberId };
            context.MemberBlocks.Add(item);
            await context.SaveChangesAsync();
        }
        return ApiResponse<MemberBlockDto>.SuccessResponse(new(item.Id, blocked.Id, $"{blocked.FirstName} {blocked.LastName}".Trim(), item.CreatedAtUtc));
    }

    public async Task<ApiResponse> UnblockAsync(Guid userId, Guid blockedMemberId)
    {
        var memberId = await MemberIdAsync(userId);
        if (memberId is null) return ApiResponse.CreateError("Member account not found");
        var item = await context.MemberBlocks.FirstOrDefaultAsync(candidate => candidate.BlockerMemberId == memberId && candidate.BlockedMemberId == blockedMemberId);
        if (item is not null) { context.MemberBlocks.Remove(item); await context.SaveChangesAsync(); }
        return ApiResponse.CreateSuccess("Member unblocked");
    }

    public async Task<int> ProcessEventRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var registrations = await context.EventRegistrations
            .Include(item => item.Event).Include(item => item.Member)
            .Where(item => item.Status == "Confirmed" && item.ReminderSentAt == null && item.Event!.Date > now && item.Event.Date <= now.AddHours(25))
            .ToListAsync(cancellationToken);
        var processed = 0;
        foreach (var registration in registrations)
        {
            var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.MemberId == registration.MemberId && item.IsActive, cancellationToken);
            if (user is null) continue;
            var preference = await context.MemberPreferences.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);
            var link = $"{PublicAppUrl()}/actualites/evenements/{registration.EventId}";
            context.Notifications.Add(new Notification { UserId = user.Id, Type = "EventReminder", Title = "Événement demain", Message = registration.Event!.Title, RelatedEntityId = registration.EventId, Link = link });
            if (preference?.EmailEvents == true)
            {
                var english = preference.PreferredLanguage == "en";
                var body = english
                    ? $"Your event {registration.Event.Title} starts on {registration.Event.Date:u}. Confirmation: {registration.ConfirmationCode}.\n{link}"
                    : $"Votre événement {registration.Event.Title} commence le {registration.Event.Date:u}. Confirmation : {registration.ConfirmationCode}.\n{link}";
                var email = emailTemplates.Newsletter(english ? $"Reminder — {registration.Event.Title}" : $"Rappel — {registration.Event.Title}", body, $"{PublicAppUrl()}/espace-membre?section=preferences", english);
                emailOutbox.Enqueue(registration.Member!.Email, email.Subject, email.HtmlBody, "EventReminder", registration.Id);
            }
            registration.ReminderSentAt = now;
            processed++;
        }
        await context.SaveChangesAsync(cancellationToken);
        return processed;
    }

    public async Task<int> ProcessWeeklyDigestsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var preferences = await context.MemberPreferences.Include(item => item.User).ThenInclude(item => item!.Member)
            .Where(item => item.DigestFrequency == "Weekly" && item.User!.IsActive && item.User.MemberId != null &&
                (!item.LastDigestSentAtUtc.HasValue || item.LastDigestSentAtUtc <= now.AddDays(-7)))
            .ToListAsync(cancellationToken);
        var events = await context.Events.AsNoTracking().Where(item => item.Date >= now && item.Status != "Draft" && item.Status != "Cancelled")
            .OrderBy(item => item.Date).Take(3).ToListAsync(cancellationToken);
        var opportunities = await context.Opportunities.AsNoTracking().Where(item => item.Status == "Published" && (!item.DeadlineUtc.HasValue || item.DeadlineUtc >= now))
            .OrderBy(item => item.DeadlineUtc ?? DateTime.MaxValue).Take(3).ToListAsync(cancellationToken);
        if (events.Count == 0 && opportunities.Count == 0) return 0;
        foreach (var preference in preferences)
        {
            var english = preference.PreferredLanguage == "en";
            var lines = new List<string> { english ? "Here is what is happening in your HCBE community this week:" : "Voici ce qui se passe dans votre communauté HCBE cette semaine :", "" };
            lines.AddRange(events.Select(item => $"• {(english ? item.TitleEn ?? item.Title : item.Title)} — {item.Date:u}"));
            lines.AddRange(opportunities.Select(item => $"• {(english ? item.TitleEn ?? item.Title : item.Title)} — {item.Organization}"));
            var subject = english ? "Your weekly HCBE community digest" : "Votre résumé communautaire HCBE";
            var email = emailTemplates.Newsletter(subject, string.Join('\n', lines), $"{PublicAppUrl()}/espace-membre?section=preferences", english);
            emailOutbox.Enqueue(preference.User!.Email, email.Subject, email.HtmlBody, "CommunityDigest", preference.UserId);
            preference.LastDigestSentAtUtc = now;
        }
        await context.SaveChangesAsync(cancellationToken);
        return preferences.Count;
    }

    public async Task<int> ProcessLifecycleJourneysAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var users = await context.Users.Include(item => item.Member).Where(item => item.IsActive && item.MemberId != null).ToListAsync(cancellationToken);
        var userIds = users.Select(item => item.Id).ToList();
        var preferences = await context.MemberPreferences.Where(item => userIds.Contains(item.UserId)).ToDictionaryAsync(item => item.UserId, cancellationToken);
        var states = await context.CommunityJourneyStates.Where(item => userIds.Contains(item.UserId)).ToListAsync(cancellationToken);
        var processed = 0;

        foreach (var user in users)
        {
            preferences.TryGetValue(user.Id, out var preference);
            var english = preference?.PreferredLanguage == "en";
            var name = user.Member?.FirstName;

            if (user.CreatedAt <= now.AddDays(-3) && preference?.HasCompletedPreferences != true &&
                !states.Any(item => item.UserId == user.Id && item.JourneyType == "OnboardingPreferences"))
            {
                var body = english
                    ? "Choose the updates you want to receive and your preferred language. You remain in control and can change these choices at any time."
                    : "Choisissez les nouvelles que vous souhaitez recevoir et votre langue préférée. Vous gardez le contrôle et pouvez modifier vos choix en tout temps.";
                var email = emailTemplates.Newsletter(english ? "Complete your HCBE communication preferences" : "Complétez vos préférences de communication HCBE", body, $"{PublicAppUrl()}/espace-membre?section=preferences", english);
                emailOutbox.Enqueue(user.Email, email.Subject, email.HtmlBody, "CommunityJourney", user.Id);
                states.Add(AddJourney(user.Id, "OnboardingPreferences", now));
                processed++;
            }

            var lastActivity = user.LastLoginAtUtc ?? user.CreatedAt;
            var lastReactivation = states.FirstOrDefault(item => item.UserId == user.Id && item.JourneyType == "Reactivation");
            if (preference?.EmailNewsletter == true && lastActivity <= now.AddDays(-60) &&
                (lastReactivation is null || lastReactivation.LastTriggeredAtUtc <= now.AddDays(-90)))
            {
                var body = english
                    ? $"Hello {name}, discover the latest events, opportunities and community services waiting for you in your member space."
                    : $"Bonjour {name}, découvrez les nouveaux événements, occasions et services communautaires disponibles dans votre espace membre.";
                var email = emailTemplates.Newsletter(english ? "Your HCBE community is waiting for you" : "Votre communauté HCBE vous attend", body, $"{PublicAppUrl()}/espace-membre", english);
                emailOutbox.Enqueue(user.Email, email.Subject, email.HtmlBody, "CommunityJourney", user.Id);
                if (lastReactivation is null) states.Add(AddJourney(user.Id, "Reactivation", now));
                else { lastReactivation.LastTriggeredAtUtc = now; lastReactivation.TriggerCount++; }
                processed++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return processed;
    }

    private CommunityJourneyState AddJourney(Guid userId, string type, DateTime now)
    {
        var state = new CommunityJourneyState { UserId = userId, JourneyType = type, LastTriggeredAtUtc = now };
        context.CommunityJourneyStates.Add(state);
        return state;
    }

    private async Task<List<SavedMemberItemDto>> ResolveSavedAsync(Guid userId)
    {
        var saved = await context.SavedMemberItems.AsNoTracking().Where(item => item.UserId == userId).OrderByDescending(item => item.CreatedAtUtc).ToListAsync();
        var eventIds = saved.Where(item => item.EntityType == "Event").Select(item => item.EntityId).ToList();
        var opportunityIds = saved.Where(item => item.EntityType == "Opportunity").Select(item => item.EntityId).ToList();
        var events = await context.Events.AsNoTracking().Where(item => eventIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id);
        var opportunities = await context.Opportunities.AsNoTracking().Where(item => opportunityIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id);
        return saved.Select(item => item.EntityType == "Event" && events.TryGetValue(item.EntityId, out var eventEntity)
                ? new SavedMemberItemDto(item.Id, item.EntityType, item.EntityId, eventEntity.Title, eventEntity.TitleEn, eventEntity.Location, eventEntity.Date, item.CreatedAtUtc)
                : item.EntityType == "Opportunity" && opportunities.TryGetValue(item.EntityId, out var opportunity)
                    ? new SavedMemberItemDto(item.Id, item.EntityType, item.EntityId, opportunity.Title, opportunity.TitleEn, opportunity.Organization, opportunity.DeadlineUtc, item.CreatedAtUtc)
                    : null)
            .Where(item => item is not null).Cast<SavedMemberItemDto>().ToList();
    }

    private Task<Guid?> MemberIdAsync(Guid userId) => context.Users.AsNoTracking().Where(item => item.Id == userId && item.IsActive).Select(item => item.MemberId).FirstOrDefaultAsync();
    private async Task<bool> IsPublishedAsync(string type, Guid id) => type == "Event"
        ? await context.Events.AnyAsync(item => item.Id == id && item.Status != "Draft" && item.Status != "Cancelled")
        : await context.Opportunities.AnyAsync(item => item.Id == id && item.Status == "Published");
    private static string? NormalizeType(string value) => SupportedTypes.FirstOrDefault(item => item.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
    private string PublicAppUrl() => (configuration["PublicAppUrl"] ?? "http://localhost:3000").TrimEnd('/');
}
