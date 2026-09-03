using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class EventCategoryEndpoints
{
    public static void MapEventCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/event-categories")
            .WithTags("Event categories")
            .WithOpenApi();

        // Inactive categories remain readable so previously published events keep
        // their human-friendly label; only the admin form filters them out.
        group.MapGet("/", async (IEventCategoryService service) =>
            (await service.GetAllAsync(true)).HandleServiceResponse());

        group.MapGet("/admin", async (HttpContext context, IEventCategoryService service) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await service.GetAllAsync(true)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPost("/", async (CreateEventCategoryRequest request, HttpContext context, IEventCategoryService service) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await service.CreateAsync(request)).ToCreatedResult("/api/event-categories");
        }).RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, UpdateEventCategoryRequest request, HttpContext context, IEventCategoryService service) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await service.UpdateAsync(id, request)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IEventCategoryService service) =>
        {
            if (!context.HasPermission(AdminPermissions.EventsManage)) return Results.Forbid();
            return (await service.DeleteAsync(id)).HandleServiceResponse();
        }).RequireAuthorization();
    }
}
