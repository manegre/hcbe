using System.Text;
using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class FinanceEndpoints
{
    public static void MapFinanceEndpoints(this WebApplication app)
    {
        var publicGroup = app.MapGroup("/api/finance").WithTags("Finance").WithOpenApi();
        publicGroup.MapGet("/plans", async (IFinanceService service, CancellationToken ct) => (await service.GetPlansAsync(false, ct)).HandleServiceResponse()).AllowAnonymous();
        publicGroup.MapGet("/campaigns", async (IFinanceService service, CancellationToken ct) => (await service.GetCampaignsAsync(false, ct)).HandleServiceResponse()).AllowAnonymous();
        publicGroup.MapPost("/donations/checkout", async (CreateDonationCheckoutRequest request, HttpContext http, IFinanceService service, CancellationToken ct) =>
            (await service.CreateDonationCheckoutAsync(http.GetUserId(), request, ct)).HandleServiceResponse()).AllowAnonymous().RequireRateLimiting("PublicWrite");
        publicGroup.MapGet("/checkout/{sessionId}", async (string sessionId, IFinanceService service, CancellationToken ct) =>
            (await service.GetCheckoutResultAsync(sessionId, ct)).HandleServiceResponse()).AllowAnonymous().RequireRateLimiting("PublicWrite");
        publicGroup.MapPost("/webhooks/stripe", async (HttpRequest request, IFinanceService service, CancellationToken ct) =>
        {
            if (request.ContentLength is > 1_048_576) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync(ct);
            var signature = request.Headers["Stripe-Signature"].ToString();
            var response = await service.ProcessWebhookAsync(payload, signature, ct);
            return response.HandleServiceResponse();
        }).AllowAnonymous().DisableAntiforgery();
        publicGroup.MapGet("/receipts/{token}", async (string token, HttpContext http, IFinanceService service, CancellationToken ct) =>
        {
            if (token.Length != 64 || !token.All(Uri.IsHexDigit)) return Results.NotFound();
            var item = await service.FindReceiptAsync(token, ct);
            if (item == null) return Results.NotFound();
            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            var pdf = ReceiptPdfRenderer.Render(item);
            return Results.File(pdf, "application/pdf", ReceiptPdfRenderer.DownloadFileName(item));
        }).AllowAnonymous().RequireRateLimiting("PublicWrite");
        publicGroup.MapGet("/membership/verify/{code}", async (string code, IFinanceService service, CancellationToken ct) =>
            (await service.VerifyMembershipAsync(code, ct)).HandleServiceResponse()).AllowAnonymous();

        var member = app.MapGroup("/api/finance/member").WithTags("Member finance").RequireAuthorization("Authenticated").WithOpenApi();
        member.MapGet("/summary", async (HttpContext http, IFinanceService service, CancellationToken ct) => http.GetUserId() is Guid userId ? (await service.GetMemberSummaryAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapGet("/membership/card", async (HttpContext http, IFinanceService service, CancellationToken ct) =>
        {
            if (http.GetUserId() is not Guid userId) return Results.Unauthorized();
            var response = await service.GetMembershipCardAsync(userId, ct);
            if (!response.Success || response.Data is null) return response.HandleServiceResponse();
            http.Response.Headers.CacheControl = "private, no-store";
            return Results.File(ReceiptPdfRenderer.RenderMembershipCard(response.Data), "application/pdf", $"HCBE-carte-membre-{DateTime.UtcNow:yyyy}.pdf");
        });
        member.MapGet("/membership/wallet", async (HttpContext http, IFinanceService service, CancellationToken ct) => http.GetUserId() is Guid userId ? (await service.GetMembershipWalletAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/membership/checkout", async (CreateMembershipCheckoutRequest request, HttpContext http, IFinanceService service, CancellationToken ct) => http.GetUserId() is Guid userId ? (await service.CreateMembershipCheckoutAsync(userId, request, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/membership/renew", async (HttpContext http, IFinanceService service, CancellationToken ct) => http.GetUserId() is Guid userId ? (await service.RenewCommunityMembershipAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/billing-portal", async (HttpContext http, IFinanceService service, CancellationToken ct) => http.GetUserId() is Guid userId ? (await service.CreateBillingPortalAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());

        var admin = app.MapGroup("/api/admin/finance").WithTags("Finance administration").RequireAuthorization().WithOpenApi();
        admin.MapGet("/dashboard", async (HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.GetDashboardAsync(ct)).HandleServiceResponse());
        admin.MapGet("/memberships", async (string? search, HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.GetMembershipsAsync(search, ct)).HandleServiceResponse());
        admin.MapGet("/transactions", async (string? status, string? kind, string? search, HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.GetTransactionsAsync(status, kind, search, ct)).HandleServiceResponse());
        admin.MapGet("/transactions/export", async (string? status, string? kind, string? search, HttpContext http, IFinanceService service, CancellationToken ct) =>
        {
            if (!http.HasPermission(AdminPermissions.FinanceManage)) return Results.Forbid();
            var response = await service.GetTransactionsAsync(status, kind, search, ct);
            if (!response.Success || response.Data == null) return response.HandleServiceResponse();
            static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
            var csv = new StringBuilder("Receipt,Kind,Status,Name,Email,Amount,Currency,Refunded,Recurring,CreatedAt,PaidAt\r\n");
            foreach (var item in response.Data) csv.Append(Csv(item.ReceiptNumber)).Append(',').Append(Csv(item.Kind)).Append(',').Append(Csv(item.Status)).Append(',').Append(Csv(item.PayerName)).Append(',').Append(Csv(item.PayerEmail)).Append(',').Append(item.AmountCents).Append(',').Append(Csv(item.Currency)).Append(',').Append(item.RefundedAmountCents).Append(',').Append(item.IsRecurring).Append(',').Append(Csv(item.CreatedAtUtc.ToString("O"))).Append(',').Append(Csv(item.PaidAtUtc?.ToString("O"))).Append("\r\n");
            return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"hcbe-finance-{DateTime.UtcNow:yyyyMMdd}.csv");
        });
        admin.MapPost("/transactions/{id:guid}/refund", async (Guid id, RefundTransactionRequest request, HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.RefundAsync(id, request, ct)).HandleServiceResponse());
        admin.MapGet("/plans", async (HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.GetPlansAsync(true, ct)).HandleServiceResponse());
        admin.MapPost("/plans", async (UpsertMembershipPlanRequest request, HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.CreatePlanAsync(request, ct)).HandleServiceResponse());
        admin.MapPut("/plans/{id:guid}", async (Guid id, UpsertMembershipPlanRequest request, HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.UpdatePlanAsync(id, request, ct)).HandleServiceResponse());
        admin.MapGet("/campaigns", async (HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.GetCampaignsAsync(true, ct)).HandleServiceResponse());
        admin.MapPost("/campaigns", async (UpsertDonationCampaignRequest request, HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.CreateCampaignAsync(request, ct)).HandleServiceResponse());
        admin.MapPut("/campaigns/{id:guid}", async (Guid id, UpsertDonationCampaignRequest request, HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.UpdateCampaignAsync(id, request, ct)).HandleServiceResponse());
        admin.MapPut("/members/{userId:guid}/standing", async (Guid userId, UpdateMembershipStandingRequest request, HttpContext http, IFinanceService service, CancellationToken ct) => !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.UpdateMembershipAsync(userId, request, ct)).HandleServiceResponse());
    }

}
