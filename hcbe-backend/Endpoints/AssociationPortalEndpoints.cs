using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class AssociationPortalEndpoints
{
    public static void MapAssociationPortalEndpoints(this IEndpointRouteBuilder app)
    {
        var member = app.MapGroup("/api/association-portal").RequireAuthorization("Authenticated").WithTags("Association portal");
        member.MapGet("/claims/me", async (HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.GetMineAsync(id)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/{associationId:guid}/claim", async (Guid associationId, CreateAssociationClaimRequest request, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.ClaimAsync(id, associationId, request)).HandleServiceResponse() : Results.Unauthorized());
        member.MapGet("/managed", async (HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.GetManagedAsync(id)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPut("/managed/{associationId:guid}", async (Guid associationId, UpdateAssociationRequest request, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.UpdateManagedAsync(id, associationId, request)).HandleServiceResponse() : Results.Unauthorized());

        var admin = app.MapGroup("/api/admin/association-claims").RequireAuthorization().WithTags("Association claim administration");
        admin.MapGet("/", async (string? status, HttpContext http, IAssociationPortalService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.GetForAdminAsync(status)).HandleServiceResponse());
        admin.MapPut("/{id:guid}", async (Guid id, ReviewAssociationClaimRequest request, HttpContext http, IAssociationPortalService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.ReviewAsync(id, request)).HandleServiceResponse());
    }
}
