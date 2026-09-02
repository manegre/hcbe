using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class MediaEndpoints
{
    private static readonly HashSet<string> AllowedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "news",
        "associations",
        "events",
        "partners",
        "cms",
    };

    public static void MapMediaEndpoints(this WebApplication app)
    {
        app.MapGet("/api/storage/{**path}", async (
            string path,
            HttpContext context,
            IFileStorageService fileStorage,
            CancellationToken cancellationToken) =>
        {
            var file = await fileStorage.ReadAsync($"/api/storage/{path}", cancellationToken);
            if (file == null) return Results.NotFound();
            context.Response.Headers.CacheControl = "public,max-age=86400";
            return Results.File(file.Bytes, file.ContentType, enableRangeProcessing: true);
        })
        .WithName("GetStoredMedia")
        .WithTags("Media")
        .AllowAnonymous()
        .Produces(200)
        .Produces(404);

        var group = app.MapGroup("/api/media")
            .WithTags("Media")
            .WithOpenApi();

        group.MapPost("/upload", async (HttpRequest request, HttpContext context, IFileStorageService fileStorage) =>
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

            var folder = form["folder"].ToString();
            if (string.IsNullOrWhiteSpace(folder) || !AllowedFolders.Contains(folder))
            {
                folder = "news";
            }

            try
            {
                if (!fileStorage.IsAllowedImageExtension(file.FileName))
                {
                    return Results.BadRequest(ApiResponse<MediaUploadDto>.ErrorResponse("Only image files are allowed"));
                }

                var (relativeUrl, _) = await fileStorage.SaveAsync(file, folder);
                var dto = new MediaUploadDto(relativeUrl, file.FileName, file.ContentType, file.Length);
                return Results.Ok(ApiResponse<MediaUploadDto>.SuccessResponse(dto));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse<MediaUploadDto>.ErrorResponse(
                    "Failed to upload file",
                    new List<string> { ex.Message }));
            }
        })
        .WithName("UploadMedia")
        .RequireAuthorization()
        .DisableAntiforgery()
        .Produces<ApiResponse<MediaUploadDto>>()
        .Produces(403)
        .Produces(400);
    }
}
