using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;
namespace HcbeApi.Endpoints;
public static class OpportunityEndpoints
{
    public static void MapOpportunityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/opportunities", async (string? type, IOpportunityService service) => (await service.GetPublishedAsync(type)).HandleServiceResponse()).AllowAnonymous().WithTags("Opportunities");
        var member = app.MapGroup("/api/opportunities").RequireAuthorization("Authenticated");
        member.MapGet("/applications/me", async (HttpContext http, IOpportunityService service) => http.GetUserId() is Guid userId ? (await service.GetMineAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/{id:guid}/apply", async (Guid id, CreateOpportunityApplicationRequest request, HttpContext http, IOpportunityService service) => http.GetUserId() is Guid userId ? (await service.ApplyAsync(userId, id, request)).HandleServiceResponse() : Results.Unauthorized());
        var admin = app.MapGroup("/api/admin/opportunities").RequireAuthorization();
        admin.MapGet("/", async (HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.GetForAdminAsync()).HandleServiceResponse());
        admin.MapPost("/", async (UpsertOpportunityRequest request, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : http.GetUserId() is Guid userId ? (await service.CreateAsync(userId, request)).HandleServiceResponse() : Results.Unauthorized());
        admin.MapPut("/{id:guid}", async (Guid id, UpsertOpportunityRequest request, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.UpdateAsync(id, request)).HandleServiceResponse());
        admin.MapDelete("/{id:guid}", async (Guid id, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.DeleteAsync(id)).HandleServiceResponse());
        admin.MapGet("/applications", async (Guid? opportunityId, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.GetApplicationsAsync(opportunityId)).HandleServiceResponse());
        admin.MapPut("/applications/{id:guid}", async (Guid id, ReviewOpportunityApplicationRequest request, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.ReviewApplicationAsync(id, request)).HandleServiceResponse());
    }
}
