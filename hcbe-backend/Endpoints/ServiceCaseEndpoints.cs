using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class ServiceCaseEndpoints
{
    public static void MapServiceCaseEndpoints(this WebApplication app)
    {
        var members = app.MapGroup("/api/service-cases")
            .WithTags("Member service requests")
            .RequireAuthorization("Authenticated")
            .WithOpenApi();

        members.MapGet("/me", async (HttpContext http, IServiceCaseService service) =>
            http.GetUserId() is Guid userId ? (await service.GetMineAsync(userId)).HandleServiceResponse() : Results.Unauthorized());
        members.MapGet("/me/{id:guid}", async (Guid id, HttpContext http, IServiceCaseService service) =>
            http.GetUserId() is Guid userId ? (await service.GetMineByIdAsync(userId, id)).HandleServiceResponse() : Results.Unauthorized());
        members.MapPost("/", async (CreateServiceCaseRequest request, HttpContext http, IServiceCaseService service) =>
            http.GetUserId() is Guid userId ? (await service.CreateAsync(userId, request)).ToCreatedResult("/api/service-cases/me") : Results.Unauthorized());
        members.MapPost("/me/{id:guid}/messages", async (Guid id, AddServiceCaseMessageRequest request, HttpContext http, IServiceCaseService service) =>
            http.GetUserId() is Guid userId ? (await service.AddMemberMessageAsync(userId, id, request with { IsInternal = false })).HandleServiceResponse() : Results.Unauthorized());
        members.MapPost("/me/{id:guid}/attachments", async (Guid id, HttpRequest request, HttpContext http, IServiceCaseService service) =>
        {
            if (http.GetUserId() is not Guid userId) return Results.Unauthorized();
            if (!request.HasFormContentType) return Results.BadRequest(ApiResponse<ServiceCaseAttachmentDto>.ErrorResponse("Request must be multipart/form-data"));
            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            return file is null || file.Length == 0
                ? Results.BadRequest(ApiResponse<ServiceCaseAttachmentDto>.ErrorResponse("No file uploaded"))
                : (await service.AddMemberAttachmentAsync(userId, id, file)).ToCreatedResult($"/api/service-cases/me/{id}");
        }).DisableAntiforgery();

        var admin = app.MapGroup("/api/admin/service-cases")
            .WithTags("Service request administration")
            .RequireAuthorization()
            .WithOpenApi();
        admin.MapGet("/", async (string? status, string? category, string? search, HttpContext http, IServiceCaseService service) =>
            !http.HasPermission(AdminPermissions.ServiceCasesManage) ? Results.Forbid() : (await service.GetForAdminAsync(status, category, search)).HandleServiceResponse());
        admin.MapGet("/{id:guid}", async (Guid id, HttpContext http, IServiceCaseService service) =>
            !http.HasPermission(AdminPermissions.ServiceCasesManage) ? Results.Forbid() : (await service.GetForAdminByIdAsync(id)).HandleServiceResponse());
        admin.MapPatch("/{id:guid}", async (Guid id, UpdateServiceCaseRequest request, HttpContext http, IServiceCaseService service) =>
            !http.HasPermission(AdminPermissions.ServiceCasesManage) ? Results.Forbid() : (await service.UpdateForAdminAsync(id, request)).HandleServiceResponse());
        admin.MapPost("/{id:guid}/messages", async (Guid id, AddServiceCaseMessageRequest request, HttpContext http, IServiceCaseService service) =>
            !http.HasPermission(AdminPermissions.ServiceCasesManage) ? Results.Forbid() : http.GetUserId() is Guid userId ? (await service.AddAdminMessageAsync(userId, id, request)).HandleServiceResponse() : Results.Unauthorized());
        admin.MapPost("/{id:guid}/attachments", async (Guid id, bool isInternal, HttpRequest request, HttpContext http, IServiceCaseService service) =>
        {
            if (!http.HasPermission(AdminPermissions.ServiceCasesManage)) return Results.Forbid();
            if (http.GetUserId() is not Guid userId) return Results.Unauthorized();
            if (!request.HasFormContentType) return Results.BadRequest(ApiResponse<ServiceCaseAttachmentDto>.ErrorResponse("Request must be multipart/form-data"));
            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            return file is null || file.Length == 0
                ? Results.BadRequest(ApiResponse<ServiceCaseAttachmentDto>.ErrorResponse("No file uploaded"))
                : (await service.AddAdminAttachmentAsync(userId, id, file, isInternal)).ToCreatedResult($"/api/admin/service-cases/{id}");
        }).DisableAntiforgery();
    }
}
