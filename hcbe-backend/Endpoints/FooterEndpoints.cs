using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class FooterEndpoints
{
    public static void MapFooterEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/footer")
            .WithTags("Footer")
            .WithOpenApi();

        group.MapGet("/", async (IFooterService footerService) =>
        {
            var response = await footerService.GetAllAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetFooterLinks")
        .Produces<ApiResponse<List<FooterLinkDto>>>()
        .Produces(400);

        group.MapGet("/admin", async (HttpContext context, IFooterService footerService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await footerService.GetAllAsync(true)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPost("/", async (CreateFooterLinkRequest request, HttpContext context, IFooterService footerService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await footerService.CreateAsync(request);
            return response.HandleServiceResponse($"/api/footer/{response.Data?.Id}");
        })
        .WithName("CreateFooterLink")
        .RequireAuthorization()
        .Produces<ApiResponse<FooterLinkDto>>()
        .Produces(403)
        .Produces(400);

        group.MapPut("/{id:guid}", async (Guid id, UpdateFooterLinkRequest request, HttpContext context, IFooterService footerService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await footerService.UpdateAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateFooterLink")
        .RequireAuthorization()
        .Produces<ApiResponse<FooterLinkDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IFooterService footerService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await footerService.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteFooterLink")
        .RequireAuthorization()
        .Produces(204)
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
