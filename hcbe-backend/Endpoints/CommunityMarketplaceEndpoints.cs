using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class CommunityMarketplaceEndpoints
{
    public static void MapCommunityMarketplaceEndpoints(this WebApplication app)
    {
        var publicGroup = app.MapGroup("/api/community-marketplace").WithTags("Community marketplace").WithOpenApi();
        publicGroup.MapGet("/ads", async (string placement, string? language, string? province, string? zone, ICommunityMarketplaceService service, CancellationToken ct) =>
            (await service.GetActiveAdsAsync(placement, language, province, zone, ct)).HandleServiceResponse()).AllowAnonymous();
        publicGroup.MapGet("/ads/{id:guid}/click", async (Guid id, ICommunityMarketplaceService service, CancellationToken ct) =>
            await service.TrackAdClickAsync(id, ct) is Uri target ? Results.Redirect(target.ToString()) : Results.NotFound()).AllowAnonymous().RequireRateLimiting("PublicWrite");

        var member = app.MapGroup("/api/community-marketplace/member").WithTags("Community organizer").RequireAuthorization("Authenticated").WithOpenApi();
        member.MapGet("/organizer", async (HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.GetMyOrganizerAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPut("/organizer", async (UpsertOrganizerRequest request, HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.SaveOrganizerAsync(userId, request, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/organizer/stripe/onboarding", async (HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.CreateOnboardingAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/organizer/stripe/refresh", async (HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.RefreshOrganizerAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapGet("/organizer/events", async (HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.GetOrganizerEventsAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/organizer/events", async (UpsertOrganizerEventRequest request, HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.SaveOrganizerEventAsync(null, userId, request, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPut("/organizer/events/{id:guid}", async (Guid id, UpsertOrganizerEventRequest request, HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.SaveOrganizerEventAsync(id, userId, request, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/ads", async (UpsertAdvertisingCampaignRequest request, HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.SaveAdAsync(null, userId, request, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapGet("/ads", async (HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.GetMyAdsAsync(userId, ct)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPut("/ads/{id:guid}", async (Guid id, UpsertAdvertisingCampaignRequest request, HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            http.GetUserId() is Guid userId ? (await service.SaveAdAsync(id, userId, request, ct)).HandleServiceResponse() : Results.Unauthorized());

        var admin = app.MapGroup("/api/admin/community-marketplace").WithTags("Community marketplace administration").RequireAuthorization().WithOpenApi();
        admin.MapGet("/organizers", async (HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.GetOrganizersAsync(ct)).HandleServiceResponse());
        admin.MapPatch("/organizers/{id:guid}", async (Guid id, ReviewOrganizerRequest request, HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.FinanceManage) ? Results.Forbid() : (await service.ReviewOrganizerAsync(id, request, ct)).HandleServiceResponse());
        admin.MapGet("/ads", async (HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.ContentManage) ? Results.Forbid() : (await service.GetAdsAsync(ct)).HandleServiceResponse());
        admin.MapPatch("/ads/{id:guid}", async (Guid id, ReviewAdvertisingCampaignRequest request, HttpContext http, ICommunityMarketplaceService service, CancellationToken ct) =>
            !http.HasPermission(AdminPermissions.ContentManage) ? Results.Forbid() : (await service.ReviewAdAsync(id, request, ct)).HandleServiceResponse());
    }
}
