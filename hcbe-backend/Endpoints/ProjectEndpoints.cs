using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .WithOpenApi();

        group.MapGet("/", async (IProjectService projectService) =>
        {
            var response = await projectService.GetAllAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetProjects")
        .Produces<ApiResponse<List<ProjectDto>>>()
        .Produces(400);

        group.MapGet("/admin", async (HttpContext context, IProjectService projectService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            var response = await projectService.GetAllForAdminAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetProjectsForAdmin")
        .RequireAuthorization()
        .Produces<ApiResponse<List<ProjectDto>>>()
        .Produces(403)
        .Produces(400);

        group.MapGet("/admin/{id:guid}", async (Guid id, HttpContext context, IProjectService projectService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage)) return Results.Forbid();
            return (await projectService.GetByIdForAdminAsync(id)).HandleServiceResponse();
        })
        .WithName("GetProjectForAdmin")
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IProjectService projectService) =>
        {
            var response = await projectService.GetByIdAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetProject")
        .Produces<ApiResponse<ProjectDto>>()
        .Produces(404)
        .Produces(400);

        group.MapPost("/", async (CreateProjectRequest request, HttpContext context, IProjectService projectService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            var response = await projectService.CreateAsync(request);
            return response.HandleServiceResponse($"/api/projects/{response.Data?.Id}");
        })
        .WithName("CreateProject")
        .RequireAuthorization()
        .Produces<ApiResponse<ProjectDto>>(201)
        .Produces(403)
        .Produces(400);

        group.MapPut("/{id:guid}", async (Guid id, UpdateProjectRequest request, HttpContext context, IProjectService projectService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            var response = await projectService.UpdateAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateProject")
        .RequireAuthorization()
        .Produces<ApiResponse<ProjectDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPut("/{id:guid}/progress", async (Guid id, UpdateProjectProgressRequest request, HttpContext context, IProjectService projectService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            var response = await projectService.UpdateProgressAsync(id, request.Progress);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateProjectProgress")
        .RequireAuthorization()
        .Produces<ApiResponse<ProjectDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IProjectService projectService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            var response = await projectService.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteProject")
        .RequireAuthorization()
        .Produces(204)
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
