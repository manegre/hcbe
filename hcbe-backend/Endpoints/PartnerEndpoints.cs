using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class PartnerEndpoints
{
    public static void MapPartnerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/partners")
            .WithTags("Partners")
            .WithOpenApi();

        group.MapGet("/", async (IPartnerService service) =>
            (await service.GetAllAsync()).HandleServiceResponse());

        group.MapGet("/admin", async (HttpContext context, IPartnerService service) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage)) return Results.Forbid();
            return (await service.GetAllAsync(true)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, HttpContext context, IPartnerService service) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage)) return Results.Forbid();
            return (await service.GetByIdAsync(id)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPost("/", async (CreatePartnerRequest request, HttpContext context, IPartnerService service) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage)) return Results.Forbid();
            return (await service.CreateAsync(request)).ToCreatedResult("/api/partners");
        }).RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, UpdatePartnerRequest request, HttpContext context, IPartnerService service) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage)) return Results.Forbid();
            return (await service.UpdateAsync(id, request)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPut("/reorder", async (ReorderPartnersRequest request, HttpContext context, IPartnerService service) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage)) return Results.Forbid();
            return (await service.ReorderAsync(request)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IPartnerService service) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage)) return Results.Forbid();
            return (await service.DeleteAsync(id)).HandleServiceResponse();
        }).RequireAuthorization();
    }
}
