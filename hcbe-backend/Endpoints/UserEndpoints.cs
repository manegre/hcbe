using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/admin", async (HttpContext context, IUserAdminService userAdminService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await userAdminService.GetAdminUsersAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetAdminUsers")
        .Produces<ApiResponse<List<AdminUserDto>>>()
        .Produces(403)
        .Produces(400);

        group.MapGet("/admin/{id:guid}", async (Guid id, HttpContext context, IUserAdminService userAdminService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await userAdminService.GetByIdAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetAdminUser")
        .Produces<ApiResponse<AdminUserDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapGet("/admin/temporary-password", (HttpContext context) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return Results.Ok(ApiResponse<string>.SuccessResponse(PasswordPolicy.GenerateTemporaryPassword()));
        })
        .WithName("GenerateAdminTemporaryPassword")
        .Produces<ApiResponse<string>>()
        .Produces(403);

        group.MapPost("/admin", async (CreateAdminUserRequest request, HttpContext context, IUserAdminService userAdminService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await userAdminService.CreateAdminUserAsync(request);
            return response.HandleServiceResponse($"/api/users/admin/{response.Data?.Id}");
        })
        .WithName("CreateAdminUser")
        .Produces<ApiResponse<AdminUserDto>>(201)
        .Produces(403)
        .Produces(400);

        group.MapPost("/admin/promote-member/{memberId:guid}", async (
            Guid memberId,
            HttpContext context,
            IUserAdminService userAdminService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await userAdminService.PromoteMemberAsync(memberId);
            return response.HandleServiceResponse();
        })
        .WithName("PromoteMemberToAdmin")
        .Produces<ApiResponse<AdminUserDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPut("/admin/{id:guid}", async (Guid id, UpdateAdminUserRequest request, HttpContext context, IUserAdminService userAdminService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var currentUserId = context.GetUserId();
            if (currentUserId == null)
            {
                return Results.Unauthorized();
            }

            var response = await userAdminService.UpdateAsync(id, request, currentUserId.Value);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateAdminUser")
        .Produces<ApiResponse<AdminUserDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/admin/{id:guid}", async (Guid id, HttpContext context, IUserAdminService userAdminService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var currentUserId = context.GetUserId();
            if (currentUserId == null)
            {
                return Results.Unauthorized();
            }

            var response = await userAdminService.DeleteAsync(id, currentUserId.Value);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteAdminUser")
        .Produces<ApiResponse<bool>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
