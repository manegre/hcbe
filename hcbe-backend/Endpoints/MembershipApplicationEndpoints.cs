using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class MembershipApplicationEndpoints
{
    public static void MapMembershipApplicationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/membership-applications")
            .WithTags("Membership Applications")
            .WithOpenApi();

        group.MapPost("/", async (CreateMembershipApplicationRequest request, IMembershipApplicationService service) =>
        {
            var response = await service.SubmitAsync(request);
            return response.HandleServiceResponse();
        })
        .RequireRateLimiting("PublicWrite")
        .WithName("SubmitMembershipApplication")
        .Produces<ApiResponse<MembershipApplicationDto>>()
        .Produces(400);

        group.MapGet("/admin", async (
            HttpContext context,
            IMembershipApplicationService service,
            string? status) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            MembershipApplicationStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<MembershipApplicationStatus>(status, true, out var parsed))
            {
                statusFilter = parsed;
            }

            var response = await service.GetAllAsync(statusFilter);
            return response.HandleServiceResponse();
        })
        .WithName("GetMembershipApplications")
        .RequireAuthorization()
        .Produces<ApiResponse<List<MembershipApplicationDto>>>()
        .Produces(403)
        .Produces(400);

        group.MapGet("/admin/paged", async (
            int page,
            int pageSize,
            string? search,
            string? sort,
            string? status,
            HttpContext context,
            IMembershipApplicationService service) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage)) return Results.Forbid();
            MembershipApplicationStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<MembershipApplicationStatus>(status, true, out var parsed)) statusFilter = parsed;
            return (await service.SearchAsync(page, pageSize, search, sort, statusFilter)).HandleServiceResponse();
        })
        .WithName("SearchMembershipApplications")
        .RequireAuthorization()
        .Produces<ApiResponse<PagedResult<MembershipApplicationDto>>>()
        .Produces(403);

        group.MapGet("/admin/{id:guid}", async (
            Guid id,
            HttpContext context,
            IMembershipApplicationService service) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await service.GetByIdAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetMembershipApplication")
        .RequireAuthorization()
        .Produces<ApiResponse<MembershipApplicationDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            HttpContext context,
            IMembershipApplicationService service) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await service.ApproveAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("ApproveMembershipApplication")
        .RequireAuthorization()
        .Produces<ApiResponse<MemberDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            HttpContext context,
            IMembershipApplicationService service) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await service.RejectAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("RejectMembershipApplication")
        .RequireAuthorization()
        .Produces<ApiResponse<MembershipApplicationDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext context,
            IMembershipApplicationService service) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await service.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteMembershipApplication")
        .RequireAuthorization()
        .Produces<ApiResponse<bool>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
