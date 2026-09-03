using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;
namespace HcbeApi.Services;
public sealed class ImpactAnalyticsService(ApplicationDbContext context) : IImpactAnalyticsService
{
    public async Task<ApiResponse<ImpactDashboardDto>> GetAsync()
    {
        var now = DateTime.UtcNow; var currentStart = now.AddDays(-30); var previousStart = now.AddDays(-60);
        var totalMembers = await context.Members.CountAsync(); var currentMembers = await context.Members.CountAsync(item => item.CreatedAt >= currentStart); var previousMembers = await context.Members.CountAsync(item => item.CreatedAt >= previousStart && item.CreatedAt < currentStart);
        var registrations = await context.EventRegistrations.CountAsync(item => item.Status != "Cancelled"); var attended = await context.EventRegistrations.CountAsync(item => item.Status == "Attended");
        var openCases = await context.ServiceCases.CountAsync(item => item.Status != "Resolved" && item.Status != "Closed"); var resolvedCases = await context.ServiceCases.Where(item => item.ResolvedAt != null).Select(item => new { item.CreatedAt, item.ResolvedAt }).ToListAsync();
        var activeMentorships = await context.MentorshipMatches.CountAsync(item => item.Status == "Active"); var completedMentorships = await context.MentorshipMatches.CountAsync(item => item.Status == "Completed");
        var opportunityApplications = await context.OpportunityApplications.CountAsync(); var managedAssociations = await context.Associations.CountAsync(item => item.OwnerMemberId != null);
        var activeUsers = await context.Users.CountAsync(item => item.IsActive && item.LastLoginAtUtc >= currentStart);
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
            new("associations", "Associations autogérées", managedAssociations, null, "associations")
        };
        var periods = new List<ImpactPeriodDto>();
        for (var offset = 5; offset >= 0; offset--)
        {
            var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-offset); var end = start.AddMonths(1);
            periods.Add(new(start.ToString("yyyy-MM"), await context.Members.CountAsync(item => item.CreatedAt >= start && item.CreatedAt < end), await context.EventRegistrations.CountAsync(item => item.RegisteredAt >= start && item.RegisteredAt < end), await context.ServiceCases.CountAsync(item => item.CreatedAt >= start && item.CreatedAt < end), await context.OpportunityApplications.CountAsync(item => item.CreatedAt >= start && item.CreatedAt < end)));
        }
        return ApiResponse<ImpactDashboardDto>.SuccessResponse(new(now, metrics, periods));
    }
    private static double? Change(int current, int previous) => previous == 0 ? current > 0 ? 100 : 0 : Math.Round((current - previous) * 100d / previous, 1);
}
