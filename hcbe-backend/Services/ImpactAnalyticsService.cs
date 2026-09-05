using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class ImpactAnalyticsService(ApplicationDbContext context) : IImpactAnalyticsService
{
    public async Task<ApiResponse<ImpactDashboardDto>> GetAsync(int months = 6)
    {
        months = months is 3 or 6 or 12 ? months : 6;
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(months - 1));
        var currentStart = now.AddDays(-30);
        var previousStart = now.AddDays(-60);
        var totalMembers = await context.Members.CountAsync();
        var currentMembers = await context.Members.CountAsync(item => item.CreatedAt >= currentStart);
        var previousMembers = await context.Members.CountAsync(item => item.CreatedAt >= previousStart && item.CreatedAt < currentStart);
        var registrations = await context.EventRegistrations.CountAsync(item => item.Status != "Cancelled");
        var attended = await context.EventRegistrations.CountAsync(item => item.Status == "Attended");
        var openCases = await context.ServiceCases.CountAsync(item => item.Status != "Resolved" && item.Status != "Closed");
        var resolvedCases = await context.ServiceCases.Where(item => item.ResolvedAt != null)
            .Select(item => new { item.CreatedAt, item.ResolvedAt }).ToListAsync();
        var activeMentorships = await context.MentorshipMatches.CountAsync(item => item.Status == "Active");
        var completedMentorships = await context.MentorshipMatches.CountAsync(item => item.Status == "Completed");
        var opportunityApplications = await context.OpportunityApplications.CountAsync();
        var managedAssociations = await context.Associations.CountAsync(item => item.OwnerMemberId != null);
        var activeUsers = await context.Users.CountAsync(item => item.IsActive && item.MemberId != null && item.LastLoginAtUtc >= currentStart);
        var savedItems = await context.SavedMemberItems.CountAsync();
        var unreadMemberNotifications = await context.Notifications.CountAsync(item => item.UserId != null && !item.IsRead);
        var weeklyDigests = await context.MemberPreferences.CountAsync(item => item.DigestFrequency == "Weekly");
        // SQLite cannot aggregate decimals, so materialize this small approved-only projection.
        var approvedVolunteerHours = (await context.VolunteerTimeEntries
            .Where(item => item.Status == "Approved")
            .Select(item => item.Hours)
            .ToListAsync()).Sum();
        var averageResolutionHours = resolvedCases.Count == 0 ? 0 : resolvedCases.Average(item => (item.ResolvedAt!.Value - item.CreatedAt).TotalHours);

        var metrics = new List<ImpactMetricDto>
        {
            new("members", "Membres de la communauté", totalMembers, Change(currentMembers, previousMembers), "membres"),
            new("active-members", "Membres actifs sur 30 jours", activeUsers, null, "membres"),
            new("event-attendance", "Taux de présence aux événements", registrations == 0 ? 0 : Math.Round(attended * 100d / registrations, 1), null, "%"),
            new("service-cases", "Demandes de service ouvertes", openCases, null, "demandes"),
            new("resolution-time", "Délai moyen de résolution", Math.Round(averageResolutionHours, 1), null, "heures"),
            new("mentorship", "Jumelages actifs", activeMentorships, null, "jumelages"),
            new("mentorship-completed", "Jumelages complétés", completedMentorships, null, "jumelages"),
            new("opportunities", "Candidatures aux occasions", opportunityApplications, null, "candidatures"),
            new("associations", "Associations autogérées", managedAssociations, null, "associations"),
            new("saved-items", "Contenus enregistrés", savedItems, null, "favoris"),
            new("unread-notifications", "Notifications membres non lues", unreadMemberNotifications, null, "notifications"),
            new("weekly-digests", "Résumés hebdomadaires actifs", weeklyDigests, null, "membres"),
            new("volunteer-hours", "Heures de bénévolat confirmées", (double)approvedVolunteerHours, null, "heures")
        };

        var periods = new List<ImpactPeriodDto>();
        for (var offset = months - 1; offset >= 0; offset--)
        {
            var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-offset);
            var end = start.AddMonths(1);
            periods.Add(new(start.ToString("yyyy-MM"),
                await context.Members.CountAsync(item => item.CreatedAt >= start && item.CreatedAt < end),
                await context.EventRegistrations.CountAsync(item => item.RegisteredAt >= start && item.RegisteredAt < end),
                await context.ServiceCases.CountAsync(item => item.CreatedAt >= start && item.CreatedAt < end),
                await context.OpportunityApplications.CountAsync(item => item.CreatedAt >= start && item.CreatedAt < end)));
        }

        var memberUsers = await context.Users.AsNoTracking().Include(item => item.Member)
            .Where(item => item.IsActive && item.MemberId != null).ToListAsync();
        var userIds = memberUsers.Select(item => item.Id).ToList();
        var memberIds = memberUsers.Where(item => item.MemberId.HasValue).Select(item => item.MemberId!.Value).ToList();
        var preferencesComplete = await context.MemberPreferences.CountAsync(item => userIds.Contains(item.UserId) && item.HasCompletedPreferences);
        var profilesComplete = memberUsers.Count(item => item.Member is not null && ProfileComplete(item.Member));
        var communityProfiles = await context.NetworkingProfiles.CountAsync(item => memberIds.Contains(item.MemberId));
        var engagedMemberIds = new HashSet<Guid>(await context.EventRegistrations.AsNoTracking()
            .Where(item => memberIds.Contains(item.MemberId) && item.Status != "Cancelled").Select(item => item.MemberId).ToListAsync());
        engagedMemberIds.UnionWith(await context.OpportunityApplications.AsNoTracking()
            .Where(item => memberIds.Contains(item.MemberId)).Select(item => item.MemberId).ToListAsync());
        engagedMemberIds.UnionWith(await context.ServiceCases.AsNoTracking()
            .Where(item => memberIds.Contains(item.MemberId)).Select(item => item.MemberId).ToListAsync());
        var denominator = Math.Max(1, memberUsers.Count);
        var funnel = new List<ActivationStageDto>
        {
            Stage("registered", "Comptes membres", memberUsers.Count, denominator),
            Stage("profile", "Profils essentiels complétés", profilesComplete, denominator),
            Stage("preferences", "Préférences configurées", preferencesComplete, denominator),
            Stage("community-profile", "Profils communautaires créés", communityProfiles, denominator),
            Stage("first-engagement", "Première participation", engagedMemberIds.Count, denominator)
        };

        var activity = new List<MemberDimensionDto>
        {
            Dimension("active", "Actifs — 30 jours", memberUsers.Count(item => item.LastLoginAtUtc >= currentStart), denominator),
            Dimension("warm", "À réengager — 31 à 60 jours", memberUsers.Count(item => item.LastLoginAtUtc < currentStart && item.LastLoginAtUtc >= previousStart), denominator),
            Dimension("dormant", "Dormants — plus de 60 jours", memberUsers.Count(item => item.LastLoginAtUtc < previousStart), denominator),
            Dimension("never", "Jamais connectés", memberUsers.Count(item => item.LastLoginAtUtc == null), denominator)
        };

        // Small groups are merged to avoid exposing a person through a low-volume export.
        var rawProvinces = memberUsers.Select(item => item.Member?.Province?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value)).GroupBy(value => value!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new { Label = group.Key, Count = group.Count() }).OrderByDescending(item => item.Count).ToList();
        var provinceDimensions = rawProvinces.Where(item => item.Count >= 3)
            .Select(item => Dimension(Slug(item.Label), item.Label, item.Count, denominator)).ToList();
        var smallCount = rawProvinces.Where(item => item.Count < 3).Sum(item => item.Count);
        if (smallCount > 0) provinceDimensions.Add(Dimension("other", "Autres régions (groupées)", smallCount, denominator));

        return ApiResponse<ImpactDashboardDto>.SuccessResponse(new(now, periodStart, months, metrics, periods, funnel, activity, provinceDimensions));
    }

    private static bool ProfileComplete(Member item) => !string.IsNullOrWhiteSpace(item.FirstName) &&
        !string.IsNullOrWhiteSpace(item.LastName) && !string.IsNullOrWhiteSpace(item.Phone) &&
        !string.IsNullOrWhiteSpace(item.City) && !string.IsNullOrWhiteSpace(item.Province) &&
        !string.IsNullOrWhiteSpace(item.Interests);
    private static ActivationStageDto Stage(string key, string label, int count, int denominator) =>
        new(key, label, count, Math.Round(count * 100d / denominator, 1));
    private static MemberDimensionDto Dimension(string key, string label, int count, int denominator) =>
        new(key, label, count, Math.Round(count * 100d / denominator, 1));
    private static string Slug(string value) => new(value.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());
    private static double? Change(int current, int previous) => previous == 0 ? current > 0 ? 100 : 0 : Math.Round((current - previous) * 100d / previous, 1);
}
