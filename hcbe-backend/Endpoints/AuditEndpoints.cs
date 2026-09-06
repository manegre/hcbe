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
            string? search,
            string? action,
            string? entityType,
            string? userEmail,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            ApplicationDbContext context,
            IConfiguration configuration,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            if (!http.HasPermission(AdminPermissions.SecurityManage)) return Results.Forbid();
            var safePage = Math.Max(page ?? 1, 1);
            var safePageSize = Math.Clamp(pageSize ?? 50, 1, 100);
            var allLogs = context.AuditLogs.AsNoTracking();
            var query = allLogs;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim()[..Math.Min(search.Trim().Length, 120)].ToLowerInvariant();
                query = query.Where(log =>
                    (log.UserEmail != null && log.UserEmail.ToLower().Contains(term)) ||
                    log.Action.ToLower().Contains(term) ||
                    log.EntityType.ToLower().Contains(term) ||
                    (log.EntityId != null && log.EntityId.ToLower().Contains(term)) ||
                    (log.TraceId != null && log.TraceId.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(log => log.Action == action);
            }

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(log => log.EntityType == entityType);
            }

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                var actor = userEmail.Trim()[..Math.Min(userEmail.Trim().Length, 254)].ToLowerInvariant();
                query = query.Where(log => log.UserEmail != null && log.UserEmail.ToLower().Contains(actor));
            }

            if (fromUtc.HasValue)
            {
                var from = fromUtc.Value.UtcDateTime;
                query = query.Where(log => log.CreatedAtUtc >= from);
            }

            if (toUtc.HasValue)
            {
                var to = toUtc.Value.UtcDateTime;
                query = query.Where(log => log.CreatedAtUtc <= to);
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(log => log.CreatedAtUtc)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var lastThirtyDays = now.AddDays(-30);
            var eventsToday = await allLogs.CountAsync(log => log.CreatedAtUtc >= now.Date, cancellationToken);
            var activeActors = await allLogs
                .Where(log => log.CreatedAtUtc >= lastThirtyDays && log.UserEmail != null)
                .Select(log => log.UserEmail)
                .Distinct()
                .CountAsync(cancellationToken);
            var securityEvents = await allLogs.CountAsync(log => log.CreatedAtUtc >= lastThirtyDays &&
                (log.Action.Contains("Mfa") || log.Action.Contains("Session") || log.EntityType == "SecurityIncident"), cancellationToken);
            var actions = await allLogs.Select(log => log.Action).Distinct().OrderBy(value => value).Take(100).ToListAsync(cancellationToken);
            var entityTypes = await allLogs.Select(log => log.EntityType).Distinct().OrderBy(value => value).Take(200).ToListAsync(cancellationToken);
            var retentionDays = Math.Clamp(configuration.GetValue("Privacy:AuditRetentionDays", 730), 90, 2555);

            var response = new
            {
                items,
                total,
                page = safePage,
                pageSize = safePageSize,
                totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)safePageSize)),
                stats = new { eventsToday, activeActors, securityEvents, retentionDays },
                filters = new { actions, entityTypes }
            };
            return Results.Ok(ApiResponse<object>.SuccessResponse(response));
        })
        .WithTags("Administration")
        .RequireAuthorization();
    }
}
