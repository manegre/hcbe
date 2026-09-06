using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class EventCommerceEndpoints
{
    public static void MapEventCommerceEndpoints(this WebApplication app)
    {
        var publicGroup = app.MapGroup("/api/event-commerce").WithTags("Event commerce").WithOpenApi();
        publicGroup.MapGet("/events/{eventId:guid}/tiers", async (Guid eventId, IEventCommerceService service, CancellationToken ct) =>
            (await service.GetTiersAsync(eventId, false, ct)).HandleServiceResponse()).AllowAnonymous();
        publicGroup.MapPost("/events/{eventId:guid}/checkout", async (Guid eventId, CreateTicketCheckoutRequest request, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            (await service.CreateCheckoutAsync(http.GetUserId(), eventId, request, ct)).HandleServiceResponse()).AllowAnonymous().RequireRateLimiting("PublicWrite");
        publicGroup.MapGet("/orders/{token}", async (string token, IEventCommerceService service, CancellationToken ct) =>
            (await service.GetOrderByTokenAsync(token, ct)).HandleServiceResponse()).AllowAnonymous().RequireRateLimiting("PublicWrite");
        publicGroup.MapGet("/orders/{token}/tickets.pdf", async (string token, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
        {
            var result = await service.BuildTicketPdfAsync(token, ct);
            if (result.Content == null) return Results.NotFound();
            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(result.Content, "application/pdf", result.FileName);
        }).AllowAnonymous().RequireRateLimiting("PublicWrite");
        publicGroup.MapPut("/orders/{token}/tickets/{ticketId:guid}/transfer", async (string token, Guid ticketId, TransferTicketRequest request, IEventCommerceService service, CancellationToken ct) =>
            (await service.TransferTicketAsync(token, ticketId, request, ct)).HandleServiceResponse()).AllowAnonymous().RequireRateLimiting("PublicWrite");

        var member = app.MapGroup("/api/event-commerce/member").WithTags("Member tickets").RequireAuthorization("Authenticated").WithOpenApi();
        member.MapGet("/orders", async (HttpContext http, IEventCommerceService service, CancellationToken ct) => http.GetUserId() is Guid userId
            ? (await service.GetMyOrdersAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());

        var admin = app.MapGroup("/api/admin/event-commerce").WithTags("Event commerce administration").RequireAuthorization().WithOpenApi();
        admin.MapGet("/events/{eventId:guid}/tiers", async (Guid eventId, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.EventsManage) ? Results.Forbid() : (await service.GetTiersAsync(eventId, true, ct)).HandleServiceResponse());
        admin.MapPost("/events/{eventId:guid}/tiers", async (Guid eventId, UpsertTicketTierRequest request, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.EventsManage) ? Results.Forbid() : (await service.CreateTierAsync(eventId, request, ct)).HandleServiceResponse());
        admin.MapPut("/events/{eventId:guid}/tiers/{tierId:guid}", async (Guid eventId, Guid tierId, UpsertTicketTierRequest request, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.EventsManage) ? Results.Forbid() : (await service.UpdateTierAsync(eventId, tierId, request, ct)).HandleServiceResponse());
        admin.MapDelete("/events/{eventId:guid}/tiers/{tierId:guid}", async (Guid eventId, Guid tierId, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.EventsManage) ? Results.Forbid() : (await service.DeleteTierAsync(eventId, tierId, ct)).HandleServiceResponse());
        admin.MapGet("/events/{eventId:guid}/promo-codes", async (Guid eventId, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.EventsManage) ? Results.Forbid() : (await service.GetPromoCodesAsync(eventId, ct)).HandleServiceResponse());
        admin.MapPost("/events/{eventId:guid}/promo-codes", async (Guid eventId, UpsertPromoCodeRequest request, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.EventsManage) ? Results.Forbid() : (await service.CreatePromoCodeAsync(eventId, request, ct)).HandleServiceResponse());
        admin.MapDelete("/events/{eventId:guid}/promo-codes/{promoId:guid}", async (Guid eventId, Guid promoId, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.EventsManage) ? Results.Forbid() : (await service.DeletePromoCodeAsync(eventId, promoId, ct)).HandleServiceResponse());
        admin.MapGet("/events/{eventId:guid}/dashboard", async (Guid eventId, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.EventsManage) ? Results.Forbid() : (await service.GetDashboardAsync(eventId, ct)).HandleServiceResponse());
        admin.MapPost("/events/{eventId:guid}/check-in/{code}", async (Guid eventId, string code, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.EventsManage) ? Results.Forbid() : (await service.CheckInAsync(eventId, code, ct)).HandleServiceResponse());
        admin.MapPost("/orders/{orderId:guid}/refund", async (Guid orderId, RefundTicketOrderRequest request, HttpContext http, IEventCommerceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.RefundAsync(orderId, request, ct)).HandleServiceResponse());
    }
}
