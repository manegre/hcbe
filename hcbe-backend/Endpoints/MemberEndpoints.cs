using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;
using System.Text;

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

        group.MapGet("/admin/export", async (HttpContext context, IMemberService memberService) =>
        {
            if (!context.HasPermission(AdminPermissions.MembersManage)) return Results.Forbid();
            var response = await memberService.GetAllAsync(); if (!response.Success || response.Data is null) return response.HandleServiceResponse();
            static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
            var csv = new StringBuilder("FirstName,LastName,Email,Phone,City,Province,Profession,Expertise,Interests,Availability,Zone,CreatedAt\r\n");
            foreach (var item in response.Data) csv.Append(Csv(item.FirstName)).Append(',').Append(Csv(item.LastName)).Append(',').Append(Csv(item.Email)).Append(',').Append(Csv(item.Phone)).Append(',').Append(Csv(item.City)).Append(',').Append(Csv(item.Province)).Append(',').Append(Csv(item.Profession)).Append(',').Append(Csv(item.Expertise)).Append(',').Append(Csv(item.Interests)).Append(',').Append(Csv(item.Availability)).Append(',').Append(Csv(item.Zone)).Append(',').Append(Csv(item.CreatedAt.ToString("O"))).Append("\r\n");
            return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", $"hcbe-members-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).WithName("ExportMembersAdmin").RequireAuthorization();

        group.MapPost("/admin/import", async (MemberImportRequest request, HttpContext context, IMemberService memberService) =>
            !context.HasPermission(AdminPermissions.MembersManage) ? Results.Forbid() : (await memberService.ImportAsync(request)).HandleServiceResponse())
            .WithName("ImportMembersAdmin").RequireAuthorization().Produces<ApiResponse<MemberImportResultDto>>().Produces(403);

        group.MapGet("/admin/duplicates", async (HttpContext context, IMemberService memberService) =>
            !context.HasPermission(AdminPermissions.MembersManage) ? Results.Forbid() : (await memberService.FindDuplicatesAsync()).HandleServiceResponse())
            .WithName("FindMemberDuplicatesAdmin").RequireAuthorization().Produces<ApiResponse<List<MemberDuplicateCandidateDto>>>().Produces(403);

        group.MapPost("/admin/merge", async (MergeMembersRequest request, HttpContext context, IMemberService memberService) =>
            !context.HasPermission(AdminPermissions.MembersManage) ? Results.Forbid() : (await memberService.MergeAsync(request.PrimaryMemberId, request.DuplicateMemberId)).HandleServiceResponse())
            .WithName("MergeMembersAdmin").RequireAuthorization().Produces<ApiResponse<MemberDto>>().Produces(403);

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
