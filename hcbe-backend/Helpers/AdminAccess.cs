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
    public const string SecurityManage = "security.manage";
    public const string PrivacyManage = "privacy.manage";

    public static readonly IReadOnlyList<string> All =
    [
        DashboardView, ContentManage, EventsManage, MembersManage, CommunityManage,
        CommunicationsManage, ServiceCasesManage, ModerationManage, AnalyticsView,
        UsersManage, SettingsManage, FinanceManage, SecurityManage, PrivacyManage
    ];
}

public sealed record AdminRoleDefinition(string Key, string Name, string NameEn, IReadOnlyCollection<string> Permissions);

public static class AdminAccess
{
    public const string SuperAdmin = "super-admin";

    public static readonly IReadOnlyList<AdminRoleDefinition> Roles =
    [
        new(SuperAdmin, "Super administrateur", "Super administrator", AdminPermissions.All),
        new("content-editor", "Éditeur de contenu", "Content editor", [AdminPermissions.DashboardView, AdminPermissions.ContentManage]),
        new("event-manager", "Responsable des événements", "Event manager", [AdminPermissions.DashboardView, AdminPermissions.EventsManage, AdminPermissions.CommunicationsManage]),
        new("community-manager", "Responsable de la communauté", "Community manager", [AdminPermissions.DashboardView, AdminPermissions.MembersManage, AdminPermissions.CommunityManage, AdminPermissions.ServiceCasesManage, AdminPermissions.ModerationManage]),
        new("communications-manager", "Responsable des communications", "Communications manager", [AdminPermissions.DashboardView, AdminPermissions.CommunicationsManage, AdminPermissions.ContentManage, AdminPermissions.AnalyticsView]),
        new("finance-manager", "Responsable des finances", "Finance manager", [AdminPermissions.DashboardView, AdminPermissions.FinanceManage, AdminPermissions.AnalyticsView]),
        new("privacy-officer", "Responsable de la protection des renseignements personnels", "Privacy officer", [AdminPermissions.DashboardView, AdminPermissions.SecurityManage, AdminPermissions.PrivacyManage, AdminPermissions.UsersManage, AdminPermissions.AnalyticsView]),
        new("security-auditor", "Auditeur sécurité", "Security auditor", [AdminPermissions.DashboardView, AdminPermissions.SecurityManage, AdminPermissions.AnalyticsView]),
        new("analyst", "Analyste / lecture seule", "Read-only analyst", [AdminPermissions.DashboardView, AdminPermissions.AnalyticsView])
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
