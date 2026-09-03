using HcbeApi.Helpers;
using HcbeApi.Services;
namespace HcbeApi.Endpoints;
public static class ImpactEndpoints
{
    public static void MapImpactEndpoints(this IEndpointRouteBuilder app) => app.MapGet("/api/admin/impact", async (HttpContext http, IImpactAnalyticsService service) => !http.HasPermission(AdminPermissions.AnalyticsView) ? Results.Forbid() : (await service.GetAsync()).HandleServiceResponse()).RequireAuthorization().WithTags("Impact analytics");
}
