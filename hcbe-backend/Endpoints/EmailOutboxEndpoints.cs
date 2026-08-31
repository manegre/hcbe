using HcbeApi.Data;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Endpoints;

public static class EmailOutboxEndpoints
{
    public static void MapEmailOutboxEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/email-outbox")
            .WithTags("Email operations")
            .RequireAuthorization();

        group.MapGet("/", async (
            string? status,
            int? page,
            int? pageSize,
            ApplicationDbContext context,
            CancellationToken cancellationToken) =>
        {
            var safePage = Math.Max(page ?? 1, 1);
            var safePageSize = Math.Clamp(pageSize ?? 50, 1, 100);
            var query = context.EmailOutboxMessages.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(message => message.Status == status);

            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(message => message.CreatedAtUtc)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Select(message => new
                {
                    message.Id,
                    message.Recipient,
                    message.Subject,
                    message.Status,
                    message.Attempts,
                    message.NextAttemptAtUtc,
                    message.CreatedAtUtc,
                    message.ProcessedAtUtc,
                    message.LastError,
                    message.RelatedEntityType,
                    message.RelatedEntityId
                })
                .ToListAsync(cancellationToken);
            return Results.Ok(new { items, total, page = safePage, pageSize = safePageSize });
        });

        group.MapPost("/{id:guid}/retry", async (
            Guid id,
            ApplicationDbContext context,
            CancellationToken cancellationToken) =>
        {
            var message = await context.EmailOutboxMessages.FindAsync([id], cancellationToken);
            if (message == null) return Results.NotFound();
            if (message.Status == "Sent") return Results.Conflict(new { message = "A sent email cannot be retried." });
            message.Status = "Pending";
            message.Attempts = 0;
            message.NextAttemptAtUtc = DateTime.UtcNow;
            message.LockedAtUtc = null;
            message.LastError = null;
            await context.SaveChangesAsync(cancellationToken);
            return Results.Accepted();
        });
    }
}
