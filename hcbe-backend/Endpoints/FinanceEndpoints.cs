using System.Net;
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
        publicGroup.MapGet("/receipts/{token}", async (string token, IFinanceService service, CancellationToken ct) =>
        {
            if (token.Length != 64 || !token.All(Uri.IsHexDigit)) return Results.NotFound();
            var item = await service.FindReceiptAsync(token, ct);
            if (item == null) return Results.NotFound();
            var html = ReceiptHtml(item);
            return Results.File(Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8", $"{item.ReceiptNumber}.html");
        }).AllowAnonymous().RequireRateLimiting("PublicWrite");
        publicGroup.MapGet("/membership/verify/{code}", async (string code, IFinanceService service, CancellationToken ct) =>
            (await service.VerifyMembershipAsync(code, ct)).HandleServiceResponse()).AllowAnonymous();

        var member = app.MapGroup("/api/finance/member").WithTags("Member finance").RequireAuthorization("Authenticated").WithOpenApi();
        member.MapGet("/summary", async (HttpContext http, IFinanceService service, CancellationToken ct) => http.GetUserId() is Guid userId ? (await service.GetMemberSummaryAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/membership/checkout", async (CreateMembershipCheckoutRequest request, HttpContext http, IFinanceService service, CancellationToken ct) => http.GetUserId() is Guid userId ? (await service.CreateMembershipCheckoutAsync(userId, request, ct)).HandleServiceResponse() : Results.Unauthorized());
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

    private static string ReceiptHtml(FinancialTransaction item)
    {
        var title = item.Kind == FinanceKinds.Membership ? "Adhésion" : "Contribution";
        var name = item.IsAnonymous ? "Donateur anonyme" : item.PayerName ?? item.PayerEmail;
        var refunded = item.RefundedAmountCents > 0
            ? $"<dt>Remboursé</dt><dd>{item.RefundedAmountCents / 100m:0.00} {WebUtility.HtmlEncode(item.Currency.ToUpperInvariant())}</dd><dt>Montant net</dt><dd class=\"amount\">{(item.AmountCents - item.RefundedAmountCents) / 100m:0.00} {WebUtility.HtmlEncode(item.Currency.ToUpperInvariant())}</dd>"
            : string.Empty;
        var status = item.Status switch
        {
            FinanceStatuses.PartiallyRefunded => "Partiellement remboursé",
            FinanceStatuses.Refunded => "Remboursé",
            FinanceStatuses.Disputed => "En litige",
            _ => "Payé"
        };
        return $$$"""
        <!doctype html><html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>{{{WebUtility.HtmlEncode(item.ReceiptNumber)}}}</title>
        <style>body{margin:0;background:#f3f5f0;color:#16251b;font:15px/1.6 Georgia,serif}main{max-width:760px;margin:48px auto;background:#fff;border:1px solid #d7dfd5;padding:48px}header{border-bottom:5px solid #f5c518;padding-bottom:28px}h1{font-size:38px;color:#0b3b21;margin:8px 0}.eyebrow{font:700 11px Arial;letter-spacing:.18em;color:#a72b1c;text-transform:uppercase}dl{display:grid;grid-template-columns:180px 1fr;margin:34px 0}dt,dd{padding:14px 0;border-bottom:1px solid #d7dfd5;margin:0}dt{font:700 11px Arial;letter-spacing:.1em;text-transform:uppercase;color:#657067}.amount{font-size:28px;font-weight:bold;color:#0b3b21}footer{font-size:12px;color:#657067;margin-top:42px}@media print{body{background:#fff}main{margin:0;border:0}}</style></head><body><main><header><div class="eyebrow">HCBE Canada · Confirmation de paiement</div><h1>Reçu de {{{WebUtility.HtmlEncode(title.ToLowerInvariant())}}}</h1><div>{{{WebUtility.HtmlEncode(item.ReceiptNumber)}}}</div></header><dl><dt>Reçu de</dt><dd>{{{WebUtility.HtmlEncode(name)}}}</dd><dt>Date</dt><dd>{{{item.PaidAtUtc!.Value:yyyy-MM-dd HH:mm}}} UTC</dd><dt>Montant initial</dt><dd class="amount">{{{item.AmountCents / 100m:0.00}}} {{{WebUtility.HtmlEncode(item.Currency.ToUpperInvariant())}}}</dd>{{{refunded}}}<dt>Statut</dt><dd>{{{WebUtility.HtmlEncode(status)}}}</dd></dl><footer><strong>HCBE Canada</strong><br>Ce reçu confirme un paiement et son état actuel. Il ne constitue pas un reçu fiscal de don de bienfaisance.<br>This receipt confirms a payment and its current status. It is not a charitable tax receipt.</footer></main></body></html>
        """;
    }
}
