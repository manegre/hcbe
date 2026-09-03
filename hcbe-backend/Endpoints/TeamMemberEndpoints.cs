using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class TeamMemberEndpoints
{
    public static void MapTeamMemberEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/team-members")
            .WithTags("Team Members")
            .WithOpenApi();

        // Public endpoints
        group.MapGet("/", async (ITeamMemberService teamMemberService) =>
        {
            var response = await teamMemberService.GetActiveAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetActiveTeamMembers")
        .Produces<ApiResponse<List<TeamMemberDto>>>()
        .Produces(400);

        group.MapGet("/{id:guid}", async (Guid id, ITeamMemberService teamMemberService) =>
        {
            var response = await teamMemberService.GetByIdAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetTeamMember")
        .Produces<ApiResponse<TeamMemberDto>>()
        .Produces(404)
        .Produces(400);

        // Admin endpoints
        group.MapGet("/admin", async (HttpContext context, ITeamMemberService teamMemberService) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage))
            {
                return Results.Forbid();
            }

            var response = await teamMemberService.GetAllAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetAllTeamMembers")
        .RequireAuthorization()
        .Produces<ApiResponse<List<TeamMemberDto>>>()
        .Produces(403)
        .Produces(400);

        group.MapPost("/", async (CreateTeamMemberRequest request, HttpContext context, ITeamMemberService teamMemberService) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage))
            {
                return Results.Forbid();
            }

            var response = await teamMemberService.CreateAsync(request);
            return response.HandleServiceResponse();
        })
        .WithName("CreateTeamMember")
        .RequireAuthorization()
        .Produces<ApiResponse<TeamMemberDto>>()
        .Produces(403)
        .Produces(400);

        group.MapPut("/{id:guid}", async (Guid id, UpdateTeamMemberRequest request, HttpContext context, ITeamMemberService teamMemberService) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage))
            {
                return Results.Forbid();
            }

            var response = await teamMemberService.UpdateAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateTeamMember")
        .RequireAuthorization()
        .Produces<ApiResponse<TeamMemberDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, ITeamMemberService teamMemberService) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage))
            {
                return Results.Forbid();
            }

            var response = await teamMemberService.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteTeamMember")
        .RequireAuthorization()
        .Produces<ApiResponse<bool>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/toggle-status", async (Guid id, HttpContext context, ITeamMemberService teamMemberService) =>
        {
            if (!context.HasPermission(AdminPermissions.ContentManage))
            {
                return Results.Forbid();
            }

            var response = await teamMemberService.ToggleStatusAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("ToggleTeamMemberStatus")
        .RequireAuthorization()
        .Produces<ApiResponse<bool>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
