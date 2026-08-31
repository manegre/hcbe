using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Services;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Endpoints;

public static class PrivacyEndpoints
{
    public static void MapPrivacyEndpoints(this WebApplication app)
    {
        var member = app.MapGroup("/api/privacy")
            .WithTags("Privacy")
            .RequireAuthorization("Authenticated");

        member.MapGet("/export", async (
            HttpContext httpContext,
            IPrivacyService service,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.GetUserId();
            if (userId == null) return Results.Unauthorized();
            var payload = await service.ExportAsync(userId.Value, cancellationToken);
            return payload == null
                ? Results.NotFound()
                : Results.File(payload, "application/json", $"hcbe-data-export-{DateTime.UtcNow:yyyyMMdd}.json");
        });

        member.MapPost("/deletion-request", async (
            HttpContext httpContext,
            IPrivacyService service,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.GetUserId();
            return userId == null
                ? Results.Unauthorized()
                : (await service.RequestDeletionAsync(userId.Value, cancellationToken)).HandleServiceResponse();
        });

        member.MapDelete("/deletion-request", async (
            HttpContext httpContext,
            IPrivacyService service,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.GetUserId();
            return userId == null
                ? Results.Unauthorized()
                : (await service.CancelDeletionAsync(userId.Value, cancellationToken)).HandleServiceResponse();
        });

        member.MapGet("/deletion-request", async (
            HttpContext httpContext,
            ApplicationDbContext context,
            CancellationToken cancellationToken) =>
        {
            var userId = httpContext.GetUserId();
            if (userId == null) return Results.Unauthorized();
            var request = await context.PrivacyRequests.AsNoTracking()
                .Where(item => item.UserId == userId)
                .OrderByDescending(item => item.RequestedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            return request == null ? Results.NoContent() : Results.Ok(request);
        });

        app.MapGet("/api/admin/privacy-requests", async (
            string? status,
            ApplicationDbContext context,
            CancellationToken cancellationToken) =>
        {
            var query = context.PrivacyRequests.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
            return Results.Ok(await query.OrderByDescending(item => item.RequestedAtUtc).Take(200).ToListAsync(cancellationToken));
        })
        .WithTags("Privacy administration")
        .RequireAuthorization();
    }
}
