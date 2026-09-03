using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Endpoints;

public static class ErrorIncidentEndpoints
{
    public static void MapErrorIncidentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/error-incidents")
            .WithTags("Production monitoring")
            .RequireAuthorization();

        group.MapGet("/", async (
            bool? includeResolved,
            HttpContext http,
            ApplicationDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!http.HasPermission(AdminPermissions.AnalyticsView)) return Results.Forbid();
            var query = db.ErrorIncidents.AsNoTracking();
            if (includeResolved != true) query = query.Where(item => item.ResolvedAtUtc == null);
            var items = await query.OrderByDescending(item => item.LastOccurredAtUtc)
                .Take(200)
                .ToListAsync(cancellationToken);
            return Results.Ok(ApiResponse<List<ErrorIncident>>.SuccessResponse(items));
        });

        group.MapPut("/{id:guid}/resolve", async (
            Guid id,
            HttpContext http,
            ApplicationDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!http.HasPermission(AdminPermissions.AnalyticsView)) return Results.Forbid();
            var incident = await db.ErrorIncidents.FindAsync([id], cancellationToken);
            if (incident is null) return Results.NotFound();
            incident.ResolvedAtUtc = DateTime.UtcNow;
            incident.ResolvedByUserId = http.GetUserId();
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });
    }
}
