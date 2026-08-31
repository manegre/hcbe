using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class GrantEndpoints
{
    public static void MapGrantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/grants")
            .WithTags("Grants")
            .WithOpenApi();

        group.MapGet("/", async (IGrantService grantService) =>
        {
            var response = await grantService.GetActiveAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetGrantPrograms")
        .Produces<ApiResponse<List<GrantProgramDto>>>()
        .Produces(400);

        group.MapGet("/admin", async (HttpContext context, IGrantService grantService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await grantService.GetAllForAdminAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetGrantProgramsForAdmin")
        .RequireAuthorization()
        .Produces<ApiResponse<List<GrantProgramDto>>>()
        .Produces(403)
        .Produces(400);

        group.MapGet("/admin/{id:guid}", async (Guid id, HttpContext context, IGrantService grantService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await grantService.GetByIdForAdminAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetGrantProgramForAdmin")
        .RequireAuthorization()
        .Produces<ApiResponse<GrantProgramDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapGet("/{id:guid}", async (Guid id, IGrantService grantService) =>
        {
            var response = await grantService.GetByIdAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetGrantProgram")
        .Produces<ApiResponse<GrantProgramDto>>()
        .Produces(404)
        .Produces(400);

        group.MapPost("/", async (CreateGrantProgramRequest request, HttpContext context, IGrantService grantService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await grantService.CreateAsync(request);
            return response.HandleServiceResponse($"/api/grants/{response.Data?.Id}");
        })
        .WithName("CreateGrantProgram")
        .RequireAuthorization()
        .Produces<ApiResponse<GrantProgramDto>>(201)
        .Produces(403)
        .Produces(400);

        group.MapPut("/{id:guid}", async (Guid id, UpdateGrantProgramRequest request, HttpContext context, IGrantService grantService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await grantService.UpdateAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateGrantProgram")
        .RequireAuthorization()
        .Produces<ApiResponse<GrantProgramDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IGrantService grantService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await grantService.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteGrantProgram")
        .RequireAuthorization()
        .Produces<ApiResponse<bool>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/toggle-status", async (Guid id, HttpContext context, IGrantService grantService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await grantService.ToggleStatusAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("ToggleGrantProgramStatus")
        .RequireAuthorization()
        .Produces<ApiResponse<bool>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
