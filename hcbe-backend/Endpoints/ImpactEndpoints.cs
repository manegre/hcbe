using System.Text;
using HcbeApi.Helpers;
using HcbeApi.Services;
namespace HcbeApi.Endpoints;
public static class ImpactEndpoints
{
    public static void MapImpactEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/impact", async (int? months, HttpContext http, IImpactAnalyticsService service) =>
            !http.HasPermission(AdminPermissions.AnalyticsView) ? Results.Forbid() : (await service.GetAsync(months ?? 6)).HandleServiceResponse())
            .RequireAuthorization().WithTags("Impact analytics");
        app.MapGet("/api/admin/impact/export", async (int? months, HttpContext http, IImpactAnalyticsService service) =>
        {
            if (!http.HasPermission(AdminPermissions.AnalyticsView)) return Results.Forbid();
            var response = await service.GetAsync(months ?? 6);
            if (!response.Success || response.Data is null) return response.HandleServiceResponse();
            static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
            var csv = new StringBuilder("Section,Key,Label,Value,Unit,Percentage,ChangePercent,Period\r\n");
            csv.Append("Report,period,").Append(Csv("Reporting period")).Append(',').Append(response.Data.PeriodMonths).Append(",months,,,").Append(response.Data.PeriodStartUtc.ToString("yyyy-MM-dd")).Append("\r\n");
            foreach (var item in response.Data.Metrics) csv.Append("Metric,").Append(Csv(item.Key)).Append(',').Append(Csv(item.Label)).Append(',').Append(item.Value).Append(',').Append(Csv(item.Unit)).Append(",,").Append(item.ChangePercent).Append(",\r\n");
            foreach (var item in response.Data.Periods)
            {
                csv.Append("Monthly activity,new-members,").Append(Csv("New members")).Append(',').Append(item.NewMembers).Append(",members,,,").Append(item.Period).Append("\r\n");
                csv.Append("Monthly activity,event-registrations,").Append(Csv("Event registrations")).Append(',').Append(item.EventRegistrations).Append(",registrations,,,").Append(item.Period).Append("\r\n");
                csv.Append("Monthly activity,service-requests,").Append(Csv("Service requests")).Append(',').Append(item.ServiceRequests).Append(",requests,,,").Append(item.Period).Append("\r\n");
                csv.Append("Monthly activity,opportunity-applications,").Append(Csv("Opportunity applications")).Append(',').Append(item.OpportunityApplications).Append(",applications,,,").Append(item.Period).Append("\r\n");
            }
            foreach (var item in response.Data.ActivationFunnel) csv.Append("Activation,").Append(Csv(item.Key)).Append(',').Append(Csv(item.Label)).Append(',').Append(item.Count).Append(",members,").Append(item.Percentage).Append(",,\r\n");
            foreach (var item in response.Data.ActivitySegments) csv.Append("Activity,").Append(Csv(item.Key)).Append(',').Append(Csv(item.Label)).Append(',').Append(item.Count).Append(",members,").Append(item.Percentage).Append(",,\r\n");
            foreach (var item in response.Data.ProvinceBreakdown) csv.Append("Province,").Append(Csv(item.Key)).Append(',').Append(Csv(item.Label)).Append(',').Append(item.Count).Append(",members,").Append(item.Percentage).Append(",,\r\n");
            return Results.File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"hcbe-impact-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization().WithTags("Impact analytics");
        app.MapGet("/api/admin/impact/report.pdf", async (int? months, HttpContext http, IImpactAnalyticsService service) =>
        {
            if (!http.HasPermission(AdminPermissions.AnalyticsView)) return Results.Forbid();
            var response = await service.GetAsync(months ?? 6);
            if (!response.Success || response.Data is null) return response.HandleServiceResponse();
            return Results.File(ReceiptPdfRenderer.RenderImpactReport(response.Data), "application/pdf", $"HCBE-rapport-impact-{DateTime.UtcNow:yyyyMMdd}.pdf");
        }).RequireAuthorization().WithTags("Impact analytics");
    }
}
