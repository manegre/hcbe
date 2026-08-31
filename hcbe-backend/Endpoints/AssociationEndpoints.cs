using Microsoft.AspNetCore.Mvc;
using HcbeApi.Services;
using HcbeApi.Models;
using HcbeApi.Helpers;

namespace HcbeApi.Endpoints;

public static class AssociationEndpoints
{
    public static void MapAssociationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/associations", GetAllAssociations)
            .WithName("GetAllAssociations")
            .WithTags("Associations");

        app.MapGet("/api/admin/associations", GetAllAssociationsForAdmin)
            .WithName("GetAllAssociationsForAdmin")
            .WithTags("Associations")
            .RequireAuthorization();

        app.MapGet("/api/associations/{id:guid}", GetAssociationById)
            .WithName("GetAssociationById")
            .WithTags("Associations");

        app.MapGet("/api/admin/associations/{id:guid}", GetAssociationByIdForAdmin)
            .WithName("GetAssociationByIdForAdmin")
            .WithTags("Associations")
            .RequireAuthorization();

        app.MapPost("/api/associations", CreateAssociation)
            .WithName("CreateAssociation")
            .WithTags("Associations")
            .RequireAuthorization();

        app.MapPut("/api/associations/{id:guid}", UpdateAssociation)
            .WithName("UpdateAssociation")
            .WithTags("Associations")
            .RequireAuthorization();

        app.MapDelete("/api/associations/{id:guid}", DeleteAssociation)
            .WithName("DeleteAssociation")
            .WithTags("Associations")
            .RequireAuthorization();

        app.MapPost("/api/associations/{id:guid}/image", UploadAssociationImage)
            .WithName("UploadAssociationImage")
            .WithTags("Associations")
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private static async Task<IResult> GetAllAssociations(
        IAssociationService associationService)
    {
        var result = await associationService.GetAllAsync();
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAssociationById(
        Guid id,
        IAssociationService associationService)
    {
        var result = await associationService.GetByIdAsync(id);
        if (!result.Success)
        {
            return Results.NotFound(result);
        }
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateAssociation(
        [FromBody] CreateAssociationRequest request,
        IAssociationService associationService,
        HttpContext context)
    {
        if (!context.IsAdmin())
        {
            return Results.Forbid();
        }

        var result = await associationService.CreateAsync(request);
        if (!result.Success)
        {
            return Results.BadRequest(result);
        }
        return Results.Created($"/api/associations/{result.Data!.Id}", result);
    }

    private static async Task<IResult> UpdateAssociation(
        Guid id,
        [FromBody] UpdateAssociationRequest request,
        IAssociationService associationService,
        HttpContext context)
    {
        if (!context.IsAdmin())
        {
            return Results.Forbid();
        }

        var result = await associationService.UpdateAsync(id, request);
        if (!result.Success)
        {
            return Results.NotFound(result);
        }
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteAssociation(
        Guid id,
        IAssociationService associationService,
        HttpContext context)
    {
        if (!context.IsAdmin())
        {
            return Results.Forbid();
        }

        var result = await associationService.DeleteAsync(id);
        if (!result.Success)
        {
            return Results.NotFound(result);
        }
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAllAssociationsForAdmin(
        IAssociationService associationService,
        HttpContext context)
    {
        if (!context.IsAdmin())
        {
            return Results.Forbid();
        }

        var result = await associationService.GetAllForAdminAsync();
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAssociationByIdForAdmin(
        Guid id,
        IAssociationService associationService,
        HttpContext context)
    {
        if (!context.IsAdmin())
        {
            return Results.Forbid();
        }

        var result = await associationService.GetByIdForAdminAsync(id);
        if (!result.Success)
        {
            return Results.NotFound(result);
        }
        return Results.Ok(result);
    }

    private static async Task<IResult> UploadAssociationImage(
        Guid id,
        HttpRequest request,
        IAssociationService associationService,
        HttpContext context)
    {
        if (!context.IsAdmin())
        {
            return Results.Forbid();
        }

        if (!request.HasFormContentType)
        {
            return Results.BadRequest(ApiResponse<MediaUploadDto>.ErrorResponse("Request must be multipart/form-data"));
        }

        var form = await request.ReadFormAsync();
        var file = form.Files["file"];
        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(ApiResponse<MediaUploadDto>.ErrorResponse("No file uploaded"));
        }

        var result = await associationService.UploadImageAsync(id, file);
        if (!result.Success)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }
}
