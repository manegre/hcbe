using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class ConsultationEndpoints
{
    public static void MapConsultationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/consultations")
            .WithTags("Consultations")
            .WithOpenApi();

        group.MapGet("/", async (HttpContext context, IConsultationService consultationService) =>
        {
            var response = await consultationService.GetActiveAsync(context.GetUserId());
            return response.HandleServiceResponse();
        })
        .WithName("GetConsultations")
        .Produces<ApiResponse<List<ConsultationDto>>>()
        .Produces(400);

        group.MapGet("/admin", async (HttpContext context, IConsultationService consultationService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            var response = await consultationService.GetAllForAdminAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetConsultationsForAdmin")
        .RequireAuthorization()
        .Produces<ApiResponse<List<ConsultationDto>>>()
        .Produces(403)
        .Produces(400);

        group.MapGet("/admin/{id:guid}", async (Guid id, HttpContext context, IConsultationService consultationService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            var response = await consultationService.GetByIdForAdminAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetConsultationForAdmin")
        .RequireAuthorization()
        .Produces<ApiResponse<ConsultationDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapGet("/{id:guid}", async (Guid id, HttpContext context, IConsultationService consultationService) =>
        {
            var response = await consultationService.GetByIdAsync(id, context.GetUserId());
            return response.HandleServiceResponse();
        })
        .WithName("GetConsultation")
        .Produces<ApiResponse<ConsultationDto>>()
        .Produces(404)
        .Produces(400);

        group.MapPost("/", async (CreateConsultationRequest request, HttpContext context, IConsultationService consultationService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            if (context.GetUserId() is not Guid userId) return Results.Unauthorized();
            var response = await consultationService.CreateAsync(request, userId);
            return response.HandleServiceResponse($"/api/consultations/{response.Data?.Id}");
        })
        .WithName("CreateConsultation")
        .RequireAuthorization()
        .Produces<ApiResponse<ConsultationDto>>(201)
        .Produces(403)
        .Produces(400);

        group.MapPut("/{id:guid}", async (Guid id, UpdateConsultationRequest request, HttpContext context, IConsultationService consultationService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            if (context.GetUserId() is not Guid userId) return Results.Unauthorized();
            var response = await consultationService.UpdateAsync(id, request, userId);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateConsultation")
        .RequireAuthorization()
        .Produces<ApiResponse<ConsultationDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IConsultationService consultationService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            var response = await consultationService.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteConsultation")
        .RequireAuthorization()
        .Produces<ApiResponse<bool>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/toggle-status", async (Guid id, HttpContext context, IConsultationService consultationService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage))
            {
                return Results.Forbid();
            }

            var response = await consultationService.ToggleStatusAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("ToggleConsultationStatus")
        .RequireAuthorization()
        .Produces<ApiResponse<bool>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/vote", async (Guid id, CastConsultationVoteRequest request, HttpContext context, IConsultationService consultationService) =>
        {
            if (context.GetUserId() is not Guid userId) return Results.Unauthorized();
            return (await consultationService.VoteAsync(id, userId, request)).HandleServiceResponse();
        })
        .WithName("VoteInConsultation")
        .RequireAuthorization("Authenticated")
        .RequireRateLimiting("PublicWrite");

        group.MapPost("/{id:guid}/comments", async (Guid id, AddConsultationCommentRequest request, HttpContext context, IConsultationService consultationService) =>
        {
            if (context.GetUserId() is not Guid userId) return Results.Unauthorized();
            return (await consultationService.CommentAsync(id, userId, request)).HandleServiceResponse();
        })
        .WithName("CommentOnConsultation")
        .RequireAuthorization("Authenticated")
        .RequireRateLimiting("PublicWrite");

        group.MapPut("/{id:guid}/results", async (Guid id, PublishConsultationResultsRequest request, HttpContext context, IConsultationService consultationService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage)) return Results.Forbid();
            if (context.GetUserId() is not Guid userId) return Results.Unauthorized();
            return (await consultationService.PublishResultsAsync(id, userId, request.Publish)).HandleServiceResponse();
        })
        .WithName("PublishConsultationResults")
        .RequireAuthorization();

        group.MapGet("/admin/{id:guid}/audit", async (Guid id, HttpContext context, IConsultationService consultationService) =>
        {
            if (!context.HasPermission(AdminPermissions.CommunityManage)) return Results.Forbid();
            return (await consultationService.GetAuditAsync(id)).HandleServiceResponse();
        })
        .WithName("GetConsultationAudit")
        .RequireAuthorization();
    }
}
