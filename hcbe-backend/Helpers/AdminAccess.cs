namespace HcbeApi.Helpers;

public static class AdminPermissions
{
    public const string DashboardView = "dashboard.view";
    public const string ContentManage = "content.manage";
    public const string EventsManage = "events.manage";
    public const string MembersManage = "members.manage";
    public const string CommunityManage = "community.manage";
    public const string CommunicationsManage = "communications.manage";
    public const string ServiceCasesManage = "service-cases.manage";
    public const string ModerationManage = "moderation.manage";
    public const string AnalyticsView = "analytics.view";
    public const string UsersManage = "users.manage";
    public const string SettingsManage = "settings.manage";
    public const string FinanceManage = "finance.manage";

    public static readonly IReadOnlyList<string> All =
    [
        DashboardView, ContentManage, EventsManage, MembersManage, CommunityManage,
        CommunicationsManage, ServiceCasesManage, ModerationManage, AnalyticsView,
        UsersManage, SettingsManage, FinanceManage
    ];
}

public sealed record AdminRoleDefinition(string Key, string Name, IReadOnlyCollection<string> Permissions);

public static class AdminAccess
{
    public const string SuperAdmin = "super-admin";

    public static readonly IReadOnlyList<AdminRoleDefinition> Roles =
    [
        new(SuperAdmin, "Super administrateur", AdminPermissions.All),
        new("content-editor", "Éditeur de contenu", [AdminPermissions.DashboardView, AdminPermissions.ContentManage]),
        new("event-manager", "Responsable des événements", [AdminPermissions.DashboardView, AdminPermissions.EventsManage, AdminPermissions.CommunicationsManage]),
        new("community-manager", "Responsable de la communauté", [AdminPermissions.DashboardView, AdminPermissions.MembersManage, AdminPermissions.CommunityManage, AdminPermissions.ServiceCasesManage, AdminPermissions.ModerationManage]),
        new("communications-manager", "Responsable des communications", [AdminPermissions.DashboardView, AdminPermissions.CommunicationsManage, AdminPermissions.ContentManage, AdminPermissions.AnalyticsView]),
        new("finance-manager", "Responsable des finances", [AdminPermissions.DashboardView, AdminPermissions.FinanceManage, AdminPermissions.AnalyticsView]),
        new("analyst", "Analyste / lecture seule", [AdminPermissions.DashboardView, AdminPermissions.AnalyticsView])
    ];

    public static bool IsValidRole(string? role) => Roles.Any(item => item.Key.Equals(role, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> EffectivePermissions(string? role, string? storedPermissions)
    {
        if (string.Equals(role, SuperAdmin, StringComparison.OrdinalIgnoreCase)) return AdminPermissions.All;
        if (!string.IsNullOrWhiteSpace(storedPermissions))
        {
            return storedPermissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(AdminPermissions.All.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToList();
        }

        return Roles.FirstOrDefault(item => item.Key.Equals(role, StringComparison.OrdinalIgnoreCase))?.Permissions.ToList()
            ?? [];
    }

    public static string SerializePermissions(IEnumerable<string>? permissions) => string.Join(',',
        (permissions ?? []).Where(AdminPermissions.All.Contains).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item));
}
