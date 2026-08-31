using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public DocumentService(ApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<ApiResponse<List<DocumentDto>>> GetAllAsync()
    {
        try
        {
            var documents = await _context.Documents
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            var documentDtos = documents.Select(MapToDto).ToList();
            return ApiResponse<List<DocumentDto>>.SuccessResponse(documentDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<DocumentDto>>.ErrorResponse(
                "Failed to retrieve documents",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<DocumentDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.IsActive);
            if (document == null)
            {
                return ApiResponse<DocumentDto>.ErrorResponse("Document not found");
            }

            return ApiResponse<DocumentDto>.SuccessResponse(MapToDto(document));
        }
        catch (Exception ex)
        {
            return ApiResponse<DocumentDto>.ErrorResponse(
                "Failed to retrieve document",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<DocumentDto>>> GetAllForAdminAsync()
    {
        try
        {
            var documents = await _context.Documents.OrderBy(d => d.DisplayOrder).ToListAsync();
            return ApiResponse<List<DocumentDto>>.SuccessResponse(documents.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<DocumentDto>>.ErrorResponse("Failed to retrieve documents", new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<DocumentDto>> GetByIdForAdminAsync(Guid id)
    {
        try
        {
            var document = await _context.Documents.FindAsync(id);
            return document is null
                ? ApiResponse<DocumentDto>.ErrorResponse("Document not found")
                : ApiResponse<DocumentDto>.SuccessResponse(MapToDto(document));
        }
        catch (Exception ex)
        {
            return ApiResponse<DocumentDto>.ErrorResponse("Failed to retrieve document", new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<DocumentDto>> UploadAsync(
        IFormFile file,
        string name,
        string? description,
        string? icon,
        string? pages,
        string? category,
        int displayOrder,
        string? nameEn = null,
        string? descriptionEn = null,
        string? pagesEn = null,
        string? categoryEn = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return ApiResponse<DocumentDto>.ErrorResponse("No file uploaded");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return ApiResponse<DocumentDto>.ErrorResponse("Document name is required");
            }

            var (storedUrl, _) = await _fileStorage.SaveAsync(file, "documents");

            // Calculate file size in MB if larger than 1MB, otherwise in KB
            var fileSizeBytes = file.Length;
            var fileSize = fileSizeBytes >= 1048576 
                ? $"{fileSizeBytes / 1048576.0:F1} MB" 
                : $"{fileSizeBytes / 1024.0:F1} KB";

            var document = new Document
            {
                Name = name.Trim(),
                NameEn = NormalizeOptional(nameEn),
                Description = NormalizeOptional(description),
                DescriptionEn = NormalizeOptional(descriptionEn),
                Icon = icon,
                Type = Path.GetExtension(file.FileName),
                Size = fileSize,
                Pages = NormalizeOptional(pages),
                PagesEn = NormalizeOptional(pagesEn),
                Category = NormalizeOptional(category),
                CategoryEn = NormalizeOptional(categoryEn),
                Url = storedUrl,
                DisplayOrder = displayOrder
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return ApiResponse<DocumentDto>.SuccessResponse(MapToDto(document));
        }
        catch (Exception ex)
        {
            return ApiResponse<DocumentDto>.ErrorResponse(
                "Failed to upload document",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<DocumentDto>> UpdateAsync(
        Guid id,
        IFormFile? file,
        string? name,
        string? description,
        string? icon,
        string? pages,
        string? category,
        int? displayOrder,
        bool? isActive,
        string? nameEn = null,
        string? descriptionEn = null,
        string? pagesEn = null,
        string? categoryEn = null)
    {
        try
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return ApiResponse<DocumentDto>.ErrorResponse("Document not found");
            }

            // Si un nouveau fichier est fourni, remplacer l'ancien
            if (file != null && file.Length > 0)
            {
                var previousUrl = document.Url;
                var (storedUrl, _) = await _fileStorage.SaveAsync(file, "documents");

                // Mettre à jour les informations du fichier
                var fileSizeBytes = file.Length;
                document.Size = fileSizeBytes >= 1048576 
                    ? $"{fileSizeBytes / 1048576.0:F1} MB" 
                    : $"{fileSizeBytes / 1024.0:F1} KB";
                document.Type = Path.GetExtension(file.FileName);
                document.Url = storedUrl;
                if (!string.IsNullOrWhiteSpace(previousUrl)) await _fileStorage.DeleteAsync(previousUrl);
            }

            // Mettre à jour les métadonnées
            if (name != null) document.Name = name;
            if (nameEn != null) document.NameEn = NormalizeOptional(nameEn);
            if (description != null) document.Description = NormalizeOptional(description);
            if (descriptionEn != null) document.DescriptionEn = NormalizeOptional(descriptionEn);
            if (icon != null) document.Icon = icon;
            if (pages != null) document.Pages = NormalizeOptional(pages);
            if (pagesEn != null) document.PagesEn = NormalizeOptional(pagesEn);
            if (category != null) document.Category = NormalizeOptional(category);
            if (categoryEn != null) document.CategoryEn = NormalizeOptional(categoryEn);
            if (displayOrder.HasValue) document.DisplayOrder = displayOrder.Value;
            if (isActive.HasValue) document.IsActive = isActive.Value;

            await _context.SaveChangesAsync();

            return ApiResponse<DocumentDto>.SuccessResponse(MapToDto(document));
        }
        catch (Exception ex)
        {
            return ApiResponse<DocumentDto>.ErrorResponse(
                "Failed to update document",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return ApiResponse.CreateError("Document not found");
            }

            if (!string.IsNullOrEmpty(document.Url)) await _fileStorage.DeleteAsync(document.Url);

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return ApiResponse.CreateSuccess("Document deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to delete document",
                new List<string> { ex.Message });
        }
    }

    public async Task<(byte[] fileBytes, string fileName, string contentType)?> GetFileForDownloadAsync(Guid id, bool includeInactive = false)
    {
        try
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null || (!includeInactive && !document.IsActive) || string.IsNullOrEmpty(document.Url))
            {
                return null;
            }

            var storedFile = await _fileStorage.ReadAsync(document.Url);
            if (storedFile == null) return null;
            document.Downloads++;
            await _context.SaveChangesAsync();

            var contentType = storedFile.ContentType == "application/octet-stream"
                ? GetContentType(document.Type)
                : storedFile.ContentType;
            return (storedFile.Bytes, document.Name, contentType);
        }
        catch
        {
            return null;
        }
    }

    private static string GetContentType(string? fileExtension)
    {
        return fileExtension?.ToLower() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }

    private static DocumentDto MapToDto(Document document)
    {
        return new DocumentDto(
            document.Id,
            document.Name,
            document.Description,
            document.Icon,
            document.Type,
            document.Size,
            document.Pages,
            document.Category,
            document.Url,
            document.Downloads,
            document.IsActive,
            document.DisplayOrder,
            document.CreatedAt,
            document.NameEn,
            document.DescriptionEn,
            document.PagesEn,
            document.CategoryEn
        );
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

