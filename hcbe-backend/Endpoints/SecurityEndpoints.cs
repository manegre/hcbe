using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Endpoints;

public static class SecurityEndpoints
{
    private const string RefreshCookieName = "hcbe_refresh";
    private static readonly string[] Severities = ["Low", "Medium", "High", "Critical"];
    private static readonly string[] Statuses = ["Reported", "Assessing", "Contained", "Recovering", "Resolved"];

    public static void MapSecurityEndpoints(this WebApplication app)
    {
        var account = app.MapGroup("/api/security").WithTags("Account security").RequireAuthorization("Authenticated");

        account.MapGet("/mfa", async (HttpContext http, ISecurityService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.GetMfaStatusAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        account.MapPost("/mfa/enroll", async (HttpContext http, ISecurityService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.BeginEnrollmentAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        account.MapPost("/mfa/confirm", async (ConfirmMfaEnrollmentRequest request, HttpContext http, ISecurityService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.ConfirmEnrollmentAsync(userId, request.Code, ct)).HandleServiceResponse() : Results.Unauthorized());
        account.MapPost("/mfa/disable", async (DisableMfaRequest request, HttpContext http, ISecurityService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.DisableMfaAsync(userId, request.Code, ct)).HandleServiceResponse() : Results.Unauthorized());
        account.MapGet("/sessions", async (HttpContext http, ISecurityService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.GetSessionsAsync(userId, http.Request.Cookies[RefreshCookieName], ct)).HandleServiceResponse() : Results.Unauthorized());
        account.MapDelete("/sessions/{id:guid}", async (Guid id, HttpContext http, ISecurityService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.RevokeSessionAsync(userId, id, http.Connection.RemoteIpAddress?.ToString(), ct)).HandleServiceResponse() : Results.Unauthorized());
        account.MapPost("/sessions/revoke-others", async (HttpContext http, ISecurityService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.RevokeOtherSessionsAsync(userId, http.Request.Cookies[RefreshCookieName], http.Connection.RemoteIpAddress?.ToString(), ct)).HandleServiceResponse() : Results.Unauthorized());

        var admin = app.MapGroup("/api/admin/security").WithTags("Administrative security").RequireAuthorization();
        admin.MapGet("/posture", async (HttpContext http, ISecurityService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.SecurityManage) ? Results.Forbid() : (await service.GetPostureAsync(ct)).HandleServiceResponse());
        admin.MapGet("/audit", async (int? page, int? pageSize, string? action, HttpContext http, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!http.HasPermission(AdminPermissions.SecurityManage)) return Results.Forbid();
            var safePage = Math.Max(page ?? 1, 1); var safePageSize = Math.Clamp(pageSize ?? 50, 1, 100);
            var query = db.AuditLogs.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(item => item.Action.Contains(action));
            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(item => item.CreatedAtUtc).Skip((safePage - 1) * safePageSize).Take(safePageSize).ToListAsync(ct);
            return Results.Ok(ApiResponse<object>.SuccessResponse(new { items, total, page = safePage, pageSize = safePageSize }));
        });
        admin.MapGet("/incidents", async (bool? includeResolved, HttpContext http, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!http.HasPermission(AdminPermissions.SecurityManage)) return Results.Forbid();
            var query = db.SecurityIncidents.AsNoTracking();
            if (includeResolved != true) query = query.Where(item => item.ResolvedAtUtc == null);
            return Results.Ok(ApiResponse<List<SecurityIncident>>.SuccessResponse(await query.OrderByDescending(item => item.ReportedAtUtc).Take(250).ToListAsync(ct)));
        });
        admin.MapPost("/incidents", async (CreateSecurityIncidentRequest request, HttpContext http, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!http.HasPermission(AdminPermissions.SecurityManage)) return Results.Forbid();
            if (http.GetUserId() is not Guid userId) return Results.Unauthorized();
            var severity = Normalize(request.Severity, Severities);
            if (severity is null || request.EstimatedPeopleAffected < 0) return Results.BadRequest(ApiResponse<SecurityIncident>.ErrorResponse("Invalid incident details"));
            var incident = new SecurityIncident
            {
                ReferenceNumber = $"INC-{DateTime.UtcNow:yyyyMMdd}-{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(3))}",
                Title = request.Title.Trim(), Description = request.Description.Trim(), Severity = severity,
                PersonalDataInvolved = request.PersonalDataInvolved, EstimatedPeopleAffected = request.EstimatedPeopleAffected,
                HarmRiskAssessment = request.HarmRiskAssessment?.Trim(), ReportedByUserId = userId, LastUpdatedByUserId = userId
            };
            db.SecurityIncidents.Add(incident); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/admin/security/incidents/{incident.Id}", ApiResponse<SecurityIncident>.SuccessResponse(incident));
        });
        admin.MapPut("/incidents/{id:guid}", async (Guid id, UpdateSecurityIncidentRequest request, HttpContext http, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!http.HasPermission(AdminPermissions.SecurityManage)) return Results.Forbid();
            if (http.GetUserId() is not Guid userId) return Results.Unauthorized();
            var item = await db.SecurityIncidents.FindAsync([id], ct); if (item is null) return Results.NotFound();
            if (request.Status is not null) { var status = Normalize(request.Status, Statuses); if (status is null) return Results.BadRequest(ApiResponse<SecurityIncident>.ErrorResponse("Invalid status")); item.Status = status; if (status == "Contained" && item.ContainedAtUtc is null) item.ContainedAtUtc = DateTime.UtcNow; if (status == "Resolved" && item.ResolvedAtUtc is null) item.ResolvedAtUtc = DateTime.UtcNow; }
            if (request.Severity is not null) { var severity = Normalize(request.Severity, Severities); if (severity is null) return Results.BadRequest(ApiResponse<SecurityIncident>.ErrorResponse("Invalid severity")); item.Severity = severity; }
            item.AssignedTo = request.AssignedTo?.Trim() ?? item.AssignedTo; item.ContainmentActions = request.ContainmentActions?.Trim() ?? item.ContainmentActions;
            item.RootCause = request.RootCause?.Trim() ?? item.RootCause; item.CorrectiveActions = request.CorrectiveActions?.Trim() ?? item.CorrectiveActions;
            item.CaiNotificationRequired = request.CaiNotificationRequired ?? item.CaiNotificationRequired; item.CaiNotifiedAtUtc = request.CaiNotifiedAtUtc ?? item.CaiNotifiedAtUtc; item.IndividualsNotifiedAtUtc = request.IndividualsNotifiedAtUtc ?? item.IndividualsNotifiedAtUtc;
            item.LastUpdatedByUserId = userId; item.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<SecurityIncident>.SuccessResponse(item));
        });
        admin.MapPost("/access-reviews/{userId:guid}", async (Guid userId, AdminAccessReviewRequest request, HttpContext http, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!http.HasPermission(AdminPermissions.UsersManage) || !http.HasPermission(AdminPermissions.SecurityManage)) return Results.Forbid();
            if (http.GetUserId() is not Guid reviewerId) return Results.Unauthorized();
            if (!await db.Users.AnyAsync(item => item.Id == userId && item.IsAdmin, ct)) return Results.NotFound();
            var decision = request.Decision.Trim(); if (decision is not ("Retain" or "Modify" or "Remove")) return Results.BadRequest(ApiResponse<AdminAccessReview>.ErrorResponse("Invalid review decision"));
            var review = new AdminAccessReview { ReviewedUserId = userId, ReviewedByUserId = reviewerId, Decision = decision, Notes = request.Notes?.Trim() };
            db.AdminAccessReviews.Add(review); await db.SaveChangesAsync(ct); return Results.Ok(ApiResponse<AdminAccessReview>.SuccessResponse(review));
        });
    }

    private static string? Normalize(string value, IReadOnlyCollection<string> values) => values.FirstOrDefault(item => item.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
}
