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
        member.MapGet("/memberships/me", async (HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.GetMyJoinRequestsAsync(id)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/{associationId:guid}/join", async (Guid associationId, CreateAssociationJoinRequest request, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.JoinAsync(id, associationId, request)).HandleServiceResponse() : Results.Unauthorized());
        member.MapGet("/managed/{associationId:guid}/workspace", async (Guid associationId, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.GetWorkspaceAsync(id, associationId)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPut("/managed/{associationId:guid}/requests/{requestId:guid}", async (Guid associationId, Guid requestId, ReviewAssociationJoinRequest request, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.ReviewJoinAsync(id, associationId, requestId, request)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPut("/managed/{associationId:guid}/members/{memberId:guid}", async (Guid associationId, Guid memberId, UpdateAssociationMemberRequest request, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.UpdateMemberAsync(id, associationId, memberId, request)).HandleServiceResponse() : Results.Unauthorized());
        member.MapDelete("/managed/{associationId:guid}/members/{memberId:guid}", async (Guid associationId, Guid memberId, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.RemoveMemberAsync(id, associationId, memberId)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/managed/{associationId:guid}/documents", async (Guid associationId, HttpRequest request, HttpContext http, IAssociationPortalService service) =>
        {
            if (http.GetUserId() is not Guid userId) return Results.Unauthorized();
            if (!request.HasFormContentType) return Results.BadRequest(ApiResponse<AssociationDocumentDto>.ErrorResponse("Request must be multipart/form-data"));
            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            if (file is null || file.Length == 0) return Results.BadRequest(ApiResponse<AssociationDocumentDto>.ErrorResponse("No file uploaded"));
            var metadata = new CreateAssociationDocumentRequest(form["title"].ToString(), form["titleEn"].ToString(), form["description"].ToString(), form["descriptionEn"].ToString(), form["visibility"].ToString());
            return (await service.AddDocumentAsync(userId, associationId, file, metadata)).HandleServiceResponse();
        }).DisableAntiforgery();
        member.MapDelete("/managed/{associationId:guid}/documents/{documentId:guid}", async (Guid associationId, Guid documentId, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.DeleteDocumentAsync(id, associationId, documentId)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/managed/{associationId:guid}/calendar", async (Guid associationId, CreateAssociationCalendarItemRequest request, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.AddCalendarItemAsync(id, associationId, request)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPut("/managed/{associationId:guid}/calendar/{itemId:guid}", async (Guid associationId, Guid itemId, CreateAssociationCalendarItemRequest request, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.UpdateCalendarItemAsync(id, associationId, itemId, request)).HandleServiceResponse() : Results.Unauthorized());
        member.MapDelete("/managed/{associationId:guid}/calendar/{itemId:guid}", async (Guid associationId, Guid itemId, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.DeleteCalendarItemAsync(id, associationId, itemId)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/managed/{associationId:guid}/service-cases/{caseId:guid}/messages", async (Guid associationId, Guid caseId, AddServiceCaseMessageRequest request, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.AddServiceCaseMessageAsync(id, associationId, caseId, request)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPatch("/managed/{associationId:guid}/service-cases/{caseId:guid}", async (Guid associationId, Guid caseId, UpdateAssociationServiceCaseRequest request, HttpContext http, IAssociationPortalService service) => http.GetUserId() is Guid id ? (await service.UpdateServiceCaseAsync(id, associationId, caseId, request)).HandleServiceResponse() : Results.Unauthorized());

        var admin = app.MapGroup("/api/admin/association-claims").RequireAuthorization().WithTags("Association claim administration");
        admin.MapGet("/", async (string? status, HttpContext http, IAssociationPortalService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.GetForAdminAsync(status)).HandleServiceResponse());
        admin.MapPut("/{id:guid}", async (Guid id, ReviewAssociationClaimRequest request, HttpContext http, IAssociationPortalService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.ReviewAsync(id, request)).HandleServiceResponse());

        var workspaces = app.MapGroup("/api/admin/association-workspaces").RequireAuthorization().WithTags("Organization workspace administration");
        workspaces.MapGet("/{associationId:guid}", async (Guid associationId, HttpContext http, IAssociationPortalService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.GetWorkspaceForAdminAsync(associationId)).HandleServiceResponse());
        workspaces.MapPut("/{associationId:guid}/requests/{requestId:guid}", async (Guid associationId, Guid requestId, ReviewAssociationJoinRequest request, HttpContext http, IAssociationPortalService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : http.GetUserId() is Guid userId ? (await service.ReviewJoinForAdminAsync(userId, associationId, requestId, request)).HandleServiceResponse() : Results.Unauthorized());
        workspaces.MapPut("/{associationId:guid}/members", async (Guid associationId, UpsertAssociationMemberRequest request, HttpContext http, IAssociationPortalService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.UpsertMemberForAdminAsync(associationId, request)).HandleServiceResponse());
        workspaces.MapDelete("/{associationId:guid}/members/{memberId:guid}", async (Guid associationId, Guid memberId, HttpContext http, IAssociationPortalService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.RemoveMemberForAdminAsync(associationId, memberId)).HandleServiceResponse());
    }
}
