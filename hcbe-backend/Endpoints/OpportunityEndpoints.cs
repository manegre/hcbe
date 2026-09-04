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
        member.MapGet("/matched", async (string? type, HttpContext http, IOpportunityService service) => http.GetUserId() is Guid userId ? (await service.GetMatchedAsync(userId, type)).HandleServiceResponse() : Results.Unauthorized());
        member.MapGet("/applications/me", async (HttpContext http, IOpportunityService service) => http.GetUserId() is Guid userId ? (await service.GetMineAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/{id:guid}/apply", async (Guid id, CreateOpportunityApplicationRequest request, HttpContext http, IOpportunityService service) => http.GetUserId() is Guid userId ? (await service.ApplyAsync(userId, id, request)).HandleServiceResponse() : Results.Unauthorized());
        member.MapPost("/applications/{applicationId:guid}/documents", async (Guid applicationId, HttpRequest request, HttpContext http, IOpportunityService service) =>
        {
            if (http.GetUserId() is not Guid userId) return Results.Unauthorized();
            if (!request.HasFormContentType) return Results.BadRequest(ApiResponse<OpportunityApplicationDocumentDto>.ErrorResponse("Request must be multipart/form-data"));
            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            return file is null || file.Length == 0
                ? Results.BadRequest(ApiResponse<OpportunityApplicationDocumentDto>.ErrorResponse("No file uploaded"))
                : (await service.AddApplicationDocumentAsync(userId, applicationId, file)).HandleServiceResponse();
        }).DisableAntiforgery();
        member.MapDelete("/applications/{applicationId:guid}/documents/{documentId:guid}", async (Guid applicationId, Guid documentId, HttpContext http, IOpportunityService service) => http.GetUserId() is Guid userId ? (await service.DeleteApplicationDocumentAsync(userId, applicationId, documentId)).HandleServiceResponse() : Results.Unauthorized());
        member.MapGet("/applications/{applicationId:guid}/documents/{documentId:guid}/download", async (Guid applicationId, Guid documentId, HttpContext http, IOpportunityService service) =>
        {
            if (http.GetUserId() is not Guid userId) return Results.Unauthorized();
            var response = await service.GetApplicationDocumentAsync(userId, applicationId, documentId, http.HasPermission(AdminPermissions.CommunityManage));
            return response.Success && response.Data is not null
                ? Results.File(response.Data.Bytes, response.Data.ContentType, response.Data.FileName)
                : Results.NotFound(response);
        });
        member.MapPost("/applications/{applicationId:guid}/hours", async (Guid applicationId, CreateVolunteerTimeEntryRequest request, HttpContext http, IOpportunityService service) => http.GetUserId() is Guid userId ? (await service.AddVolunteerTimeAsync(userId, applicationId, request)).HandleServiceResponse() : Results.Unauthorized());
        member.MapGet("/applications/{applicationId:guid}/certificate", async (Guid applicationId, HttpContext http, IOpportunityService service) =>
        {
            if (http.GetUserId() is not Guid userId) return Results.Unauthorized();
            var response = await service.GetCertificatePdfAsync(userId, applicationId, http.HasPermission(AdminPermissions.CommunityManage));
            return response.Success && response.Data is not null
                ? Results.File(response.Data, "application/pdf", $"attestation-hcbe-{applicationId:N}.pdf")
                : Results.NotFound(response);
        });
        var admin = app.MapGroup("/api/admin/opportunities").RequireAuthorization();
        admin.MapGet("/", async (HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.GetForAdminAsync()).HandleServiceResponse());
        admin.MapPost("/", async (UpsertOpportunityRequest request, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : http.GetUserId() is Guid userId ? (await service.CreateAsync(userId, request)).HandleServiceResponse() : Results.Unauthorized());
        admin.MapPut("/{id:guid}", async (Guid id, UpsertOpportunityRequest request, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.UpdateAsync(id, request)).HandleServiceResponse());
        admin.MapDelete("/{id:guid}", async (Guid id, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.DeleteAsync(id)).HandleServiceResponse());
        admin.MapGet("/applications", async (Guid? opportunityId, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.GetApplicationsAsync(opportunityId)).HandleServiceResponse());
        admin.MapPut("/applications/{id:guid}", async (Guid id, ReviewOpportunityApplicationRequest request, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : (await service.ReviewApplicationAsync(id, request)).HandleServiceResponse());
        admin.MapPut("/hours/{id:guid}", async (Guid id, ReviewVolunteerTimeEntryRequest request, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : http.GetUserId() is Guid userId ? (await service.ReviewVolunteerTimeAsync(userId, id, request)).HandleServiceResponse() : Results.Unauthorized());
        admin.MapPost("/applications/{id:guid}/certificate", async (Guid id, IssueOpportunityCertificateRequest request, HttpContext http, IOpportunityService service) => !http.HasPermission(AdminPermissions.CommunityManage) ? Results.Forbid() : http.GetUserId() is Guid userId ? (await service.IssueCertificateAsync(userId, id, request)).HandleServiceResponse() : Results.Unauthorized());
    }
}
