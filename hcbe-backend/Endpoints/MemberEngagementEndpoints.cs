using HcbeApi.Helpers;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class MemberEngagementEndpoints
{
    public static void MapMemberEngagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/member-engagement").WithTags("Member engagement").RequireAuthorization("Authenticated").WithOpenApi();
        group.MapGet("/dashboard", async (HttpContext context, IMemberEngagementService service) =>
            context.GetUserId() is Guid userId ? (await service.GetDashboardAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapGet("/saved", async (HttpContext context, IMemberEngagementService service) =>
            context.GetUserId() is Guid userId ? (await service.GetSavedAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPut("/saved/{entityType}/{entityId:guid}", async (string entityType, Guid entityId, HttpContext context, IMemberEngagementService service) =>
            context.GetUserId() is Guid userId ? (await service.SaveAsync(userId, entityType, entityId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapDelete("/saved/{entityType}/{entityId:guid}", async (string entityType, Guid entityId, HttpContext context, IMemberEngagementService service) =>
            context.GetUserId() is Guid userId ? (await service.RemoveSavedAsync(userId, entityType, entityId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapGet("/blocks", async (HttpContext context, IMemberEngagementService service) =>
            context.GetUserId() is Guid userId ? (await service.GetBlocksAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPut("/blocks/{memberId:guid}", async (Guid memberId, HttpContext context, IMemberEngagementService service) =>
            context.GetUserId() is Guid userId ? (await service.BlockAsync(userId, memberId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapDelete("/blocks/{memberId:guid}", async (Guid memberId, HttpContext context, IMemberEngagementService service) =>
            context.GetUserId() is Guid userId ? (await service.UnblockAsync(userId, memberId)).HandleServiceResponse() : Results.Unauthorized());
    }
}
