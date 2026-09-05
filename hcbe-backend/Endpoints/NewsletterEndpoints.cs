using System.Text;
using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class NewsletterEndpoints
{
    public static void MapNewsletterEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/newsletter")
            .WithTags("Newsletter")
            .WithOpenApi();

        group.MapPost("/subscribe", async (SubscribeNewsletterRequest request, INewsletterService service) =>
        {
            var response = await service.SubscribeAsync(request);
            return response.HandleServiceResponse();
        })
        .WithName("SubscribeNewsletter")
        .RequireRateLimiting("PublicWrite")
        .Produces<ApiResponse<object>>()
        .Produces(400);

        group.MapGet("/unsubscribe", async (string token, Guid? campaignId, INewsletterService service) =>
            (await service.UnsubscribeAsync(token, campaignId)).HandleServiceResponse())
            .AllowAnonymous();

        group.MapGet("/track/open/{token}.gif", async (string token, HttpContext context, INewsletterCampaignService service, CancellationToken cancellationToken) =>
        {
            await service.TrackOpenAsync(token, cancellationToken);
            context.Response.Headers.CacheControl = "no-store, max-age=0";
            context.Response.Headers.Pragma = "no-cache";
            var pixel = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");
            return Results.File(pixel, "image/gif", enableRangeProcessing: false);
        }).AllowAnonymous().ExcludeFromDescription();

        group.MapGet("/campaigns", async (HttpContext context, INewsletterCampaignService service) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage)) return Results.Forbid();
            return (await service.GetAllAsync()).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPost("/campaigns/preview", async (
            CreateNewsletterCampaignRequest request,
            HttpContext context,
            INewsletterCampaignService service,
            CancellationToken cancellationToken) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage)) return Results.Forbid();
            return (await service.PreviewAsync(request, cancellationToken)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapGet("/campaigns/{id:guid}/deliveries", async (
            Guid id,
            HttpContext context,
            INewsletterCampaignService service,
            CancellationToken cancellationToken) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage)) return Results.Forbid();
            return (await service.GetDeliveriesAsync(id, cancellationToken)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPost("/campaigns", async (
            CreateNewsletterCampaignRequest request,
            HttpContext context,
            INewsletterCampaignService service) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage)) return Results.Forbid();
            var userId = context.GetUserId();
            return userId is null
                ? Results.Unauthorized()
                : (await service.CreateAsync(request, userId.Value)).ToCreatedResult("/api/newsletter/campaigns");
        }).RequireAuthorization();

        group.MapPost("/campaigns/{id:guid}/send", async (
            Guid id,
            HttpContext context,
            INewsletterCampaignService service,
            CancellationToken cancellationToken) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage)) return Results.Forbid();
            return (await service.SendAsync(id, cancellationToken)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPost("/campaigns/{id:guid}/test", async (
            Guid id,
            SendCampaignTestRequest request,
            HttpContext context,
            INewsletterCampaignService service,
            CancellationToken cancellationToken) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage)) return Results.Forbid();
            return (await service.SendTestAsync(id, request.Email, cancellationToken)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapGet("/subscriptions", async (
            HttpContext context,
            INewsletterService service,
            string? language,
            bool? isActive) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage))
            {
                return Results.Forbid();
            }

            var response = await service.GetAllAsync(language, isActive);
            return response.HandleServiceResponse();
        })
        .WithName("GetNewsletterSubscriptions")
        .RequireAuthorization()
        .Produces<ApiResponse<List<NewsletterSubscriptionDto>>>()
        .Produces(403)
        .Produces(400);

        group.MapGet("/subscriptions/paged", async (
            int page,
            int pageSize,
            string? search,
            string? sort,
            string? language,
            bool? isActive,
            HttpContext context,
            INewsletterService service) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage)) return Results.Forbid();
            return (await service.SearchAsync(page, pageSize, search, sort, language, isActive)).HandleServiceResponse();
        })
        .WithName("SearchNewsletterSubscriptions")
        .RequireAuthorization()
        .Produces<ApiResponse<PagedResult<NewsletterSubscriptionDto>>>()
        .Produces(403);

        group.MapPatch("/subscriptions/{id:guid}", async (
            Guid id,
            UpdateNewsletterSubscriptionRequest request,
            HttpContext context,
            INewsletterService service) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage))
            {
                return Results.Forbid();
            }

            var response = await service.UpdateActiveAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateNewsletterSubscription")
        .RequireAuthorization()
        .Produces<ApiResponse<NewsletterSubscriptionDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapGet("/subscriptions/export", async (
            HttpContext context,
            INewsletterService service) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage))
            {
                return Results.Forbid();
            }

            var response = await service.ExportActiveCsvAsync();
            if (!response.Success || response.Data == null)
            {
                return Results.BadRequest(response);
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(response.Data)).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", "newsletter-subscribers.csv");
        })
        .WithName("ExportNewsletterSubscriptions")
        .RequireAuthorization()
        .Produces(200)
        .Produces(403)
        .Produces(400);

        group.MapGet("/consents", async (int? limit, HttpContext context, INewsletterService service) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunicationsManage)) return Results.Forbid();
            return (await service.GetConsentHistoryAsync(limit ?? 100)).HandleServiceResponse();
        })
        .WithName("GetCommunicationConsentHistory")
        .RequireAuthorization();
    }
}
