using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class CommunityEndpoints
{
    public static void MapCommunityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/community").WithTags("Mentorship and networking").RequireAuthorization("Authenticated").WithOpenApi();

        group.MapGet("/mentorship/applications/me", async (HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.GetMyApplicationsAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPost("/mentorship/applications", async (CreateMentorshipApplicationRequest request, HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.ApplyForMentorshipAsync(userId, request)).ToCreatedResult("/api/community/mentorship/applications/me") : Results.Unauthorized());
        group.MapPost("/mentorship/applications/{id:guid}/withdraw", async (Guid id, HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.WithdrawApplicationAsync(userId, id)).HandleServiceResponse() : Results.Unauthorized());
        group.MapGet("/mentorship/matches/me", async (HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.GetMyMatchesAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPost("/mentorship/matches/{id:guid}/respond", async (Guid id, string response, HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.RespondToMatchAsync(userId, id, response)).HandleServiceResponse() : Results.Unauthorized());
        group.MapGet("/mentorship/matches/{id:guid}/journey", async (Guid id, HttpContext context, IMentorshipJourneyService service) => context.GetUserId() is Guid userId ? (await service.GetAsync(userId, id)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPost("/mentorship/matches/{id:guid}/goals", async (Guid id, CreateMentorshipGoalRequest request, HttpContext context, IMentorshipJourneyService service) => context.GetUserId() is Guid userId ? (await service.AddGoalAsync(userId, id, request)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPut("/mentorship/goals/{id:guid}", async (Guid id, UpdateMentorshipGoalRequest request, HttpContext context, IMentorshipJourneyService service) => context.GetUserId() is Guid userId ? (await service.UpdateGoalAsync(userId, id, request)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPost("/mentorship/matches/{id:guid}/check-ins", async (Guid id, CreateMentorshipCheckInRequest request, HttpContext context, IMentorshipJourneyService service) => context.GetUserId() is Guid userId ? (await service.AddCheckInAsync(userId, id, request)).HandleServiceResponse() : Results.Unauthorized());

        group.MapGet("/networking/profile/me", async (HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.GetMyNetworkingProfileAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPut("/networking/profile/me", async (UpsertNetworkingProfileRequest request, HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.UpsertNetworkingProfileAsync(userId, request)).HandleServiceResponse() : Results.Unauthorized());
        group.MapGet("/networking/directory", async (string? search, string? province, HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.SearchDirectoryAsync(userId, search, province)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPost("/networking/requests", async (CreateConnectionRequestRequest request, HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.CreateConnectionRequestAsync(userId, request)).ToCreatedResult("/api/community/networking/requests/me") : Results.Unauthorized());
        group.MapGet("/networking/requests/me", async (HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.GetMyConnectionRequestsAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        group.MapPost("/networking/requests/{id:guid}/respond", async (Guid id, RespondConnectionRequestRequest request, HttpContext context, ICommunityService service) =>
            context.GetUserId() is Guid userId ? (await service.RespondToConnectionRequestAsync(userId, id, request)).HandleServiceResponse() : Results.Unauthorized());

        var admin = app.MapGroup("/api/admin/mentorship").WithTags("Mentorship administration").RequireAuthorization().WithOpenApi();
        admin.MapGet("/applications", async (string? role, string? status, string? search, HttpContext context, ICommunityService service) =>
            !context.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.GetApplicationsForAdminAsync(role, status, search)).HandleServiceResponse());
        admin.MapPatch("/applications/{id:guid}", async (Guid id, ReviewMentorshipApplicationRequest request, HttpContext context, ICommunityService service) =>
            !context.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.ReviewApplicationAsync(id, request)).HandleServiceResponse());
        admin.MapGet("/matches", async (HttpContext context, ICommunityService service) =>
            !context.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.GetMatchesForAdminAsync()).HandleServiceResponse());
        admin.MapPost("/matches", async (CreateMentorshipMatchRequest request, HttpContext context, ICommunityService service) =>
            !context.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.CreateMatchAsync(request)).ToCreatedResult("/api/admin/mentorship/matches"));
        admin.MapPatch("/matches/{id:guid}/status", async (Guid id, UpdateMentorshipMatchStatusRequest request, HttpContext context, ICommunityService service) =>
            !context.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.UpdateMatchStatusAsync(id, request)).HandleServiceResponse());
        admin.MapGet("/support-flags", async (HttpContext context, IMentorshipJourneyService service) => !context.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.GetSupportFlagsAsync()).HandleServiceResponse());
    }
}
