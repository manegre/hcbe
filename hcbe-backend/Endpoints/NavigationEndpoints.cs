using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class NavigationEndpoints
{
    public static void MapNavigationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/navigation")
            .WithTags("Navigation")
            .WithOpenApi();

        group.MapGet("/", async (INavigationService navigationService) =>
        {
            var response = await navigationService.GetAllAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetNavigationItems")
        .Produces<ApiResponse<List<NavigationItemDto>>>()
        .Produces(400);

        group.MapGet("/admin", async (HttpContext context, INavigationService navigationService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await navigationService.GetAllAsync(true)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPost("/", async (CreateNavigationItemRequest request, HttpContext context, INavigationService navigationService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await navigationService.CreateAsync(request);
            return response.HandleServiceResponse($"/api/navigation/{response.Data?.Id}");
        })
        .WithName("CreateNavigationItem")
        .RequireAuthorization()
        .Produces<ApiResponse<NavigationItemDto>>()
        .Produces(403)
        .Produces(400);

        group.MapPut("/{id:guid}", async (Guid id, UpdateNavigationItemRequest request, HttpContext context, INavigationService navigationService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await navigationService.UpdateAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateNavigationItem")
        .RequireAuthorization()
        .Produces<ApiResponse<NavigationItemDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, INavigationService navigationService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await navigationService.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteNavigationItem")
        .RequireAuthorization()
        .Produces(204)
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
