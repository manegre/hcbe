using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class ContentEndpoints
{
    public static void MapContentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/content")
            .WithTags("Content")
            .WithOpenApi();

        group.MapGet("/sections", async (string? page, IContentService contentService) =>
        {
            var response = await contentService.GetPageSectionsAsync(page);
            return response.HandleServiceResponse();
        })
        .WithName("GetPageSections")
        .Produces<ApiResponse<List<PageSectionDto>>>()
        .Produces(400);

        group.MapGet("/sections/admin", async (string? page, HttpContext context, IContentService contentService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await contentService.GetPageSectionsAsync(page, true)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapGet("/services", async (IContentService contentService) =>
        {
            var response = await contentService.GetServicesAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetServices")
        .Produces<ApiResponse<List<ServiceContentDto>>>()
        .Produces(400);

        group.MapGet("/services/admin", async (HttpContext context, IContentService contentService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await contentService.GetServicesAsync(true)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPost("/sections", async (CreatePageSectionRequest request, HttpContext context, IContentService contentService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await contentService.CreatePageSectionAsync(request)).ToCreatedResult("/api/content/sections");
        }).RequireAuthorization();

        group.MapPut("/sections/{id:guid}", async (Guid id, UpdatePageSectionRequest request, HttpContext context, IContentService contentService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await contentService.UpdatePageSectionAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdatePageSection")
        .RequireAuthorization()
        .Produces<ApiResponse<PageSectionDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/sections/{id:guid}", async (Guid id, HttpContext context, IContentService contentService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await contentService.DeletePageSectionAsync(id)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPost("/services", async (CreateServiceContentRequest request, HttpContext context, IContentService contentService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await contentService.CreateServiceAsync(request)).ToCreatedResult("/api/content/services");
        }).RequireAuthorization();

        group.MapPut("/services/{id:guid}", async (Guid id, UpdateServiceContentRequest request, HttpContext context, IContentService contentService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await contentService.UpdateServiceAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateServiceContent")
        .RequireAuthorization()
        .Produces<ApiResponse<ServiceContentDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/services/{id:guid}", async (Guid id, HttpContext context, IContentService contentService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await contentService.DeleteServiceAsync(id)).HandleServiceResponse();
        }).RequireAuthorization();
    }
}
