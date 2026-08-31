using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class PublicSubmissionEndpoints
{
    public static void MapPublicSubmissionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/submissions")
            .WithTags("Public submissions")
            .WithOpenApi();

        group.MapPost("/", async (CreatePublicSubmissionRequest request, IPublicSubmissionService service) =>
            (await service.SubmitAsync(request)).ToCreatedResult("/api/submissions"))
            .AllowAnonymous()
            .RequireRateLimiting("PublicWrite")
            .Produces<ApiResponse<PublicSubmissionDto>>(201)
            .Produces(400);

        group.MapGet("/admin", async (
            string? type,
            string? status,
            HttpContext context,
            IPublicSubmissionService service) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await service.GetAllAsync(type, status)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapGet("/admin/paged", async (
            int page,
            int pageSize,
            string? search,
            string? sort,
            string? type,
            string? status,
            HttpContext context,
            IPublicSubmissionService service) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await service.SearchAsync(page, pageSize, search, sort, type, status)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapGet("/admin/{id:guid}", async (
            Guid id,
            HttpContext context,
            IPublicSubmissionService service) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await service.GetByIdAsync(id)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapPatch("/admin/{id:guid}/status", async (
            Guid id,
            UpdatePublicSubmissionStatusRequest request,
            HttpContext context,
            IPublicSubmissionService service) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await service.UpdateStatusAsync(id, request)).HandleServiceResponse();
        }).RequireAuthorization();

        group.MapDelete("/admin/{id:guid}", async (
            Guid id,
            HttpContext context,
            IPublicSubmissionService service) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await service.DeleteAsync(id)).HandleServiceResponse();
        }).RequireAuthorization();
    }
}
