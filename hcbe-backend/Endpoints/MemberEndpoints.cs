using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class MemberEndpoints
{
    public static void MapMemberEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/members")
            .WithTags("Members")
            .WithOpenApi();

        group.MapGet("/", async (HttpContext context, IMemberService memberService) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await memberService.GetAllAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetMembers")
        .RequireAuthorization()
        .Produces<ApiResponse<List<MemberDto>>>()
        .Produces(403)
        .Produces(400);

        group.MapGet("/admin", async (HttpContext context, IMemberService memberService) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await memberService.GetAllAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetAllMembersAdmin")
        .RequireAuthorization()
        .Produces<ApiResponse<List<MemberDto>>>()
        .Produces(403)
        .Produces(400);

        group.MapGet("/admin/paged", async (
            int page, int pageSize, string? search, string? sort,
            HttpContext context, IMemberService memberService) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage)) return Results.Forbid();
            return (await memberService.SearchAsync(page, pageSize, search, sort)).HandleServiceResponse();
        })
        .WithName("SearchMembersAdmin")
        .RequireAuthorization()
        .Produces<ApiResponse<PagedResult<MemberDto>>>()
        .Produces(403);

        group.MapGet("/{id:guid}", async (Guid id, HttpContext context, IMemberService memberService) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await memberService.GetByIdAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetMember")
        .RequireAuthorization()
        .Produces<ApiResponse<MemberDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/", async (CreateMemberRequest request, HttpContext context, IMemberService memberService) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await memberService.CreateAsync(request);
            return response.HandleServiceResponse();
        })
        .WithName("CreateMember")
        .RequireAuthorization()
        .Produces<ApiResponse<MemberDto>>()
        .Produces(403)
        .Produces(400);

        group.MapPut("/{id:guid}", async (Guid id, UpdateMemberRequest request, HttpContext context, IMemberService memberService) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await memberService.UpdateAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateMember")
        .RequireAuthorization()
        .Produces<ApiResponse<MemberDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IMemberService memberService) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await memberService.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteMember")
        .RequireAuthorization()
        .Produces<ApiResponse<bool>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPut("/{id:guid}/admin", async (Guid id, bool isAdmin, HttpContext context, IMemberService memberService) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage))
            {
                return Results.Forbid();
            }

            var response = await memberService.UpdateAdminStatusAsync(id, isAdmin);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateMemberAdminStatus")
        .RequireAuthorization()
        .Produces<ApiResponse<MemberDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
