using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class CmsEndpoints
{
    public static void MapCmsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/cms/content", async (ICmsContentService service) =>
            (await service.GetPublishedAsync()).HandleServiceResponse())
            .WithName("GetPublishedCmsContent")
            .WithTags("CMS")
            .AllowAnonymous()
            .Produces<ApiResponse<CmsPublishedBundleDto>>();

        var admin = app.MapGroup("/api/cms/admin")
            .WithTags("CMS Administration")
            .WithOpenApi()
            .RequireAuthorization();

        admin.MapGet("/content", async (string? page, HttpContext context, ICmsContentService service) =>
            !context.HasPermission(AdminPermissions.ContentManage)
                ? Results.Forbid()
                : (await service.GetAdminItemsAsync(page)).HandleServiceResponse());

        admin.MapPut("/content", async (UpsertCmsContentRequest request, HttpContext context, ICmsContentService service) =>
            !context.HasPermission(AdminPermissions.ContentManage)
                ? Results.Forbid()
                : (await service.UpsertAsync(request, context.GetUserId())).HandleServiceResponse());

        admin.MapPost("/content/{id:guid}/publish", async (Guid id, HttpContext context, ICmsContentService service) =>
            !context.HasPermission(AdminPermissions.ContentManage)
                ? Results.Forbid()
                : (await service.PublishAsync(id, context.GetUserId())).HandleServiceResponse());

        admin.MapPost("/publish", async (HttpContext context, ICmsContentService service) =>
            !context.HasPermission(AdminPermissions.ContentManage)
                ? Results.Forbid()
                : (await service.PublishAllAsync(context.GetUserId())).HandleServiceResponse());

        admin.MapGet("/content/{id:guid}/revisions", async (Guid id, HttpContext context, ICmsContentService service) =>
            !context.HasPermission(AdminPermissions.ContentManage)
                ? Results.Forbid()
                : (await service.GetRevisionsAsync(id)).HandleServiceResponse());

        admin.MapPost("/content/{id:guid}/rollback/{version:int}", async (Guid id, int version, HttpContext context, ICmsContentService service) =>
            !context.HasPermission(AdminPermissions.ContentManage)
                ? Results.Forbid()
                : (await service.RollbackAsync(id, version, context.GetUserId())).HandleServiceResponse());

        admin.MapDelete("/content/{id:guid}", async (Guid id, HttpContext context, ICmsContentService service) =>
            !context.HasPermission(AdminPermissions.ContentManage)
                ? Results.Forbid()
                : (await service.DeleteAsync(id)).HandleServiceResponse());
    }
}
