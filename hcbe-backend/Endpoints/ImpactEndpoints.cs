using System.Text;
using HcbeApi.Helpers;
using HcbeApi.Services;
namespace HcbeApi.Endpoints;
public static class ImpactEndpoints
{
    public static void MapImpactEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/impact", async (HttpContext http, IImpactAnalyticsService service) =>
            !http.HasPermission(AdminPermissions.AnalyticsView) ? Results.Forbid() : (await service.GetAsync()).HandleServiceResponse())
            .RequireAuthorization().WithTags("Impact analytics");
        app.MapGet("/api/admin/impact/export", async (HttpContext http, IImpactAnalyticsService service) =>
        {
            if (!http.HasPermission(AdminPermissions.AnalyticsView)) return Results.Forbid();
            var response = await service.GetAsync();
            if (!response.Success || response.Data is null) return response.HandleServiceResponse();
            static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
            var csv = new StringBuilder("Section,Key,Label,Count,Percentage\r\n");
            foreach (var item in response.Data.ActivationFunnel) csv.Append("Activation,").Append(Csv(item.Key)).Append(',').Append(Csv(item.Label)).Append(',').Append(item.Count).Append(',').Append(item.Percentage).Append("\r\n");
            foreach (var item in response.Data.ActivitySegments) csv.Append("Activity,").Append(Csv(item.Key)).Append(',').Append(Csv(item.Label)).Append(',').Append(item.Count).Append(',').Append(item.Percentage).Append("\r\n");
            foreach (var item in response.Data.ProvinceBreakdown) csv.Append("Province,").Append(Csv(item.Key)).Append(',').Append(Csv(item.Label)).Append(',').Append(item.Count).Append(',').Append(item.Percentage).Append("\r\n");
            return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"hcbe-activation-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization().WithTags("Impact analytics");
    }
}
