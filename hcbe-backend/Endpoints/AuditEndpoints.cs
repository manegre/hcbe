using HcbeApi.Data;
using Microsoft.EntityFrameworkCore;
using HcbeApi.Helpers;

namespace HcbeApi.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/audit-logs", async (
            int? page,
            int? pageSize,
            string? entityType,
            ApplicationDbContext context,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            if (!http.HasPermission(AdminPermissions.SecurityManage)) return Results.Forbid();
            var safePage = Math.Max(page ?? 1, 1);
            var safePageSize = Math.Clamp(pageSize ?? 50, 1, 100);
            var query = context.AuditLogs.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(log => log.EntityType == entityType);
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(log => log.CreatedAtUtc)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync(cancellationToken);
            return Results.Ok(new { items, total, page = safePage, pageSize = safePageSize });
        })
        .WithTags("Administration")
        .RequireAuthorization();
    }
}
