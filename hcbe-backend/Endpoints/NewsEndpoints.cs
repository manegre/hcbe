using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class NewsEndpoints
{
    public static void MapNewsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/news")
            .WithTags("News")
            .WithOpenApi();

        group.MapGet("/", async (INewsService newsService) =>
        {
            var response = await newsService.GetPublishedAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetNews")
        .Produces<ApiResponse<List<NewsDto>>>()
        .Produces(400);

        group.MapGet("/admin", async (HttpContext context, INewsService newsService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await newsService.GetAllForAdminAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetNewsForAdmin")
        .RequireAuthorization()
        .Produces<ApiResponse<List<NewsDto>>>()
        .Produces(403)
        .Produces(400);

        group.MapGet("/admin/{id:guid}", async (Guid id, HttpContext context, INewsService newsService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await newsService.GetByIdForAdminAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetNewsItemForAdmin")
        .RequireAuthorization()
        .Produces<ApiResponse<NewsDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapGet("/{id:guid}", async (Guid id, INewsService newsService) =>
        {
            var response = await newsService.GetByIdAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("GetNewsItem")
        .Produces<ApiResponse<NewsDto>>()
        .Produces(404)
        .Produces(400);

        group.MapPost("/", async (CreateNewsRequest request, HttpContext context, INewsService newsService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await newsService.CreateAsync(request);
            return response.HandleServiceResponse($"/api/news/{response.Data?.Id}");
        })
        .WithName("CreateNews")
        .RequireAuthorization()
        .Produces<ApiResponse<NewsDto>>(201)
        .Produces(403)
        .Produces(400);

        group.MapPut("/{id:guid}", async (Guid id, CreateNewsRequest request, HttpContext context, INewsService newsService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await newsService.UpdateAsync(id, request);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateNews")
        .RequireAuthorization()
        .Produces<ApiResponse<NewsDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, INewsService newsService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await newsService.DeleteAsync(id);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteNews")
        .RequireAuthorization()
        .Produces(204)
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/cover", async (Guid id, HttpRequest request, HttpContext context, INewsService newsService) =>
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

            var response = await newsService.UploadCoverImageAsync(id, file);
            return response.HandleServiceResponse();
        })
        .WithName("UploadNewsCover")
        .RequireAuthorization()
        .DisableAntiforgery()
        .Produces<ApiResponse<MediaUploadDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapPost("/{id:guid}/attachments", async (Guid id, HttpRequest request, HttpContext context, INewsService newsService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            if (!request.HasFormContentType)
            {
                return Results.BadRequest(ApiResponse<NewsAttachmentDto>.ErrorResponse("Request must be multipart/form-data"));
            }

            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(ApiResponse<NewsAttachmentDto>.ErrorResponse("No file uploaded"));
            }

            var response = await newsService.AddAttachmentAsync(id, file);
            return response.HandleServiceResponse($"/api/news/{id}/attachments/{response.Data?.Id}");
        })
        .WithName("UploadNewsAttachment")
        .RequireAuthorization()
        .DisableAntiforgery()
        .Produces<ApiResponse<NewsAttachmentDto>>(201)
        .Produces(403)
        .Produces(404)
        .Produces(400);

        group.MapDelete("/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId, HttpContext context, INewsService newsService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await newsService.DeleteAttachmentAsync(id, attachmentId);
            return response.HandleServiceResponse();
        })
        .WithName("DeleteNewsAttachment")
        .RequireAuthorization()
        .Produces(204)
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
