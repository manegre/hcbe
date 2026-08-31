using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents")
            .WithOpenApi();

        group.MapGet("/", async (IDocumentService documentService) =>
        {
            var response = await documentService.GetAllAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetDocuments")
        .Produces<ApiResponse<List<DocumentDto>>>()
        .Produces(400);

        group.MapGet("/admin", async (HttpContext context, IDocumentService documentService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await documentService.GetAllForAdminAsync()).HandleServiceResponse();
        }).WithName("GetDocumentsForAdmin").RequireAuthorization();

        group.MapGet("/admin/{id:guid}", async (Guid id, HttpContext context, IDocumentService documentService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            return (await documentService.GetByIdForAdminAsync(id)).HandleServiceResponse();
        }).WithName("GetDocumentForAdmin").RequireAuthorization();

        group.MapGet("/admin/{id:guid}/download", async (Guid id, HttpContext context, IDocumentService documentService) =>
        {
            if (!context.IsAdmin()) return Results.Forbid();
            var fileData = await documentService.GetFileForDownloadAsync(id, includeInactive: true);
            return fileData is null
                ? Results.NotFound()
                : Results.File(fileData.Value.fileBytes, fileData.Value.contentType, fileData.Value.fileName);
        }).WithName("DownloadDocumentForAdmin").RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IDocumentService documentService) =>
        {
            var response = await documentService.GetByIdAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetDocument")
        .Produces<ApiResponse<DocumentDto>>()
        .Produces(404)
        .Produces(400);

        group.MapGet("/{id:guid}/download", async (Guid id, IDocumentService documentService) =>
        {
            var fileData = await documentService.GetFileForDownloadAsync(id);
            if (fileData == null)
            {
                return Results.NotFound();
            }

            return Results.File(fileData.Value.fileBytes, fileData.Value.contentType, fileData.Value.fileName);
        })
        .WithName("DownloadDocument")
        .Produces(200)
        .Produces(404);

        group.MapPost("/", async (HttpRequest request, HttpContext context, IDocumentService documentService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            if (!request.HasFormContentType)
            {
                return Results.BadRequest(ApiResponse<DocumentDto>.ErrorResponse("Request must be multipart/form-data"));
            }

            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            var name = form["name"].ToString();
            var description = form["description"].ToString();
            var icon = form["icon"].ToString();
            var pages = form["pages"].ToString();
            var category = form["category"].ToString();
            var displayOrderStr = form["displayOrder"].ToString();
            var nameEn = form["nameEn"].ToString();
            var descriptionEn = form["descriptionEn"].ToString();
            var pagesEn = form["pagesEn"].ToString();
            var categoryEn = form["categoryEn"].ToString();

            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(ApiResponse<DocumentDto>.ErrorResponse("No file uploaded"));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.BadRequest(ApiResponse<DocumentDto>.ErrorResponse("Document name is required"));
            }

            var displayOrder = int.TryParse(displayOrderStr, out var order) ? order : 0;
            var response = await documentService.UploadAsync(
                file, name, description, icon, pages, category, displayOrder,
                nameEn, descriptionEn, pagesEn, categoryEn);
            return response.HandleServiceResponse($"/api/documents/{response.Data?.Id}");
        })
        .WithName("UploadDocument")
        .RequireAuthorization()
        .Produces<ApiResponse<DocumentDto>>(201)
        .Produces(403)
        .Produces(400);

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest request, HttpContext context, IDocumentService documentService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var form = await request.ReadFormAsync();
            var file = form.Files["file"]; // Fichier optionnel
            var name = form["name"].ToString();
            var description = form["description"].ToString();
            var icon = form["icon"].ToString();
            var pages = form["pages"].ToString();
            var category = form["category"].ToString();
            var displayOrderStr = form["displayOrder"].ToString();
            var isActiveStr = form["isActive"].ToString();
            var nameEn = form.ContainsKey("nameEn") ? form["nameEn"].ToString() : null;
            var descriptionEn = form.ContainsKey("descriptionEn") ? form["descriptionEn"].ToString() : null;
            var pagesEn = form.ContainsKey("pagesEn") ? form["pagesEn"].ToString() : null;
            var categoryEn = form.ContainsKey("categoryEn") ? form["categoryEn"].ToString() : null;

            var displayOrder = int.TryParse(displayOrderStr, out var order) ? (int?)order : null;
            var isActive = bool.TryParse(isActiveStr, out var active) ? (bool?)active : null;

            var response = await documentService.UpdateAsync(
                id, file, name, description, icon, pages, category, displayOrder, isActive,
                nameEn, descriptionEn, pagesEn, categoryEn);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateDocument")
        .RequireAuthorization()
        .Produces<ApiResponse<DocumentDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IDocumentService documentService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await documentService.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteDocument")
        .RequireAuthorization()
        .Produces(204)
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
