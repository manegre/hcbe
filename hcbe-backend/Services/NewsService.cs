using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class NewsService : INewsService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IFileStorageService _fileStorage;

    public NewsService(
        ApplicationDbContext context,
        INotificationService notificationService,
        IFileStorageService fileStorage)
    {
        _context = context;
        _notificationService = notificationService;
        _fileStorage = fileStorage;
    }

    public async Task<ApiResponse<List<NewsDto>>> GetPublishedAsync()
    {
        try
        {
            var newsItems = await _context.News
                .AsNoTracking()
                .Include(n => n.Attachments)
                .Where(n => n.Status == "published")
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.PublishedDate ?? n.CreatedAt)
                .ToListAsync();

            return ApiResponse<List<NewsDto>>.SuccessResponse(newsItems.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<NewsDto>>.ErrorResponse(
                "Failed to retrieve news articles",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<NewsDto>>> GetAllForAdminAsync()
    {
        try
        {
            var newsItems = await _context.News
                .AsNoTracking()
                .Include(n => n.Attachments)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.PublishedDate ?? n.CreatedAt)
                .ToListAsync();

            return ApiResponse<List<NewsDto>>.SuccessResponse(newsItems.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<NewsDto>>.ErrorResponse(
                "Failed to retrieve news articles",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<NewsDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var newsItem = await _context.News
                .AsNoTracking()
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id && n.Status == "published");

            if (newsItem == null)
            {
                return ApiResponse<NewsDto>.ErrorResponse("News article not found");
            }

            return ApiResponse<NewsDto>.SuccessResponse(MapToDto(newsItem));
        }
        catch (Exception ex)
        {
            return ApiResponse<NewsDto>.ErrorResponse(
                "Failed to retrieve news article",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<NewsDto>> GetByIdForAdminAsync(Guid id)
    {
        try
        {
            var newsItem = await _context.News
                .AsNoTracking()
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (newsItem == null)
            {
                return ApiResponse<NewsDto>.ErrorResponse("News article not found");
            }

            return ApiResponse<NewsDto>.SuccessResponse(MapToDto(newsItem));
        }
        catch (Exception ex)
        {
            return ApiResponse<NewsDto>.ErrorResponse(
                "Failed to retrieve news article",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<NewsDto>> CreateAsync(CreateNewsRequest request)
    {
        try
        {
            var newsItem = new News
            {
                Title = request.Title.Trim(),
                TitleEn = NormalizeOptional(request.TitleEn),
                Content = request.Content.Trim(),
                ContentEn = NormalizeOptional(request.ContentEn),
                Excerpt = request.Excerpt?.Trim(),
                ExcerptEn = NormalizeOptional(request.ExcerptEn),
                ImageUrl = request.ImageUrl?.Trim(),
                ImagePosition = NormalizeImagePosition(request.ImagePosition),
                Author = request.Author?.Trim(),
                Category = request.Category?.Trim(),
                PublishedDate = request.PublishedDate ?? DateTime.UtcNow,
                IsPinned = request.IsPinned,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.News.Add(newsItem);
            await _context.SaveChangesAsync();

            if (newsItem.Status == "published")
            {
                await _notificationService.CreateNotificationAsync(
                    "news",
                    "Nouvel article publié",
                    newsItem.Title,
                    newsItem.Id,
                    "#news"
                );
            }

            return ApiResponse<NewsDto>.SuccessResponse(MapToDto(newsItem));
        }
        catch (Exception ex)
        {
            return ApiResponse<NewsDto>.ErrorResponse(
                "Failed to create news article",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<NewsDto>> UpdateAsync(Guid id, CreateNewsRequest request)
    {
        try
        {
            var newsItem = await _context.News
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (newsItem == null)
            {
                return ApiResponse<NewsDto>.ErrorResponse("News article not found");
            }

            var previousImageUrl = newsItem.ImageUrl;
            var nextImageUrl = request.ImageUrl?.Trim();

            newsItem.Title = request.Title.Trim();
            newsItem.TitleEn = NormalizeOptional(request.TitleEn);
            newsItem.Content = request.Content.Trim();
            newsItem.ContentEn = NormalizeOptional(request.ContentEn);
            newsItem.Excerpt = request.Excerpt?.Trim();
            newsItem.ExcerptEn = NormalizeOptional(request.ExcerptEn);
            newsItem.ImageUrl = nextImageUrl;
            newsItem.ImagePosition = NormalizeImagePosition(request.ImagePosition);
            newsItem.Author = request.Author?.Trim();
            newsItem.Category = request.Category?.Trim();
            newsItem.PublishedDate = request.PublishedDate;
            newsItem.IsPinned = request.IsPinned;
            newsItem.Status = request.Status;
            newsItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (!string.Equals(previousImageUrl, nextImageUrl, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(previousImageUrl)
                && previousImageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                await _fileStorage.DeleteAsync(previousImageUrl);
            }

            return ApiResponse<NewsDto>.SuccessResponse(MapToDto(newsItem));
        }
        catch (Exception ex)
        {
            return ApiResponse<NewsDto>.ErrorResponse(
                "Failed to update news article",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try
        {
            var newsItem = await _context.News
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (newsItem == null)
            {
                return ApiResponse.CreateError("News article not found");
            }

            foreach (var attachment in newsItem.Attachments)
            {
                await _fileStorage.DeleteAsync(attachment.Url);
            }

            if (!string.IsNullOrWhiteSpace(newsItem.ImageUrl)
                && newsItem.ImageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                await _fileStorage.DeleteAsync(newsItem.ImageUrl);
            }

            _context.News.Remove(newsItem);
            await _context.SaveChangesAsync();

            return ApiResponse.CreateSuccess("News article deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to delete news article",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<MediaUploadDto>> UploadCoverImageAsync(Guid id, IFormFile file)
    {
        try
        {
            var newsItem = await _context.News.FindAsync(id);
            if (newsItem == null)
            {
                return ApiResponse<MediaUploadDto>.ErrorResponse("News article not found");
            }

            if (!_fileStorage.IsAllowedImageExtension(file.FileName))
            {
                return ApiResponse<MediaUploadDto>.ErrorResponse("Only image files are allowed for the cover");
            }

            var previousImageUrl = newsItem.ImageUrl;
            var (relativeUrl, _) = await _fileStorage.SaveAsync(file, "news");

            newsItem.ImageUrl = relativeUrl;
            newsItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(previousImageUrl)
                && previousImageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(previousImageUrl, relativeUrl, StringComparison.Ordinal))
            {
                await _fileStorage.DeleteAsync(previousImageUrl);
            }

            return ApiResponse<MediaUploadDto>.SuccessResponse(new MediaUploadDto(
                relativeUrl,
                file.FileName,
                file.ContentType,
                file.Length));
        }
        catch (Exception ex)
        {
            return ApiResponse<MediaUploadDto>.ErrorResponse(
                "Failed to upload cover image",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<NewsAttachmentDto>> AddAttachmentAsync(Guid id, IFormFile file)
    {
        try
        {
            var newsItem = await _context.News.FindAsync(id);
            if (newsItem == null)
            {
                return ApiResponse<NewsAttachmentDto>.ErrorResponse("News article not found");
            }

            var (relativeUrl, _) = await _fileStorage.SaveAsync(file, "news");
            var attachment = new NewsAttachment
            {
                NewsId = newsItem.Id,
                FileName = Path.GetFileName(file.FileName),
                Url = relativeUrl,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType,
                SizeBytes = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            _context.NewsAttachments.Add(attachment);
            newsItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<NewsAttachmentDto>.SuccessResponse(MapAttachmentToDto(attachment));
        }
        catch (Exception ex)
        {
            return ApiResponse<NewsAttachmentDto>.ErrorResponse(
                "Failed to upload attachment",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeleteAttachmentAsync(Guid newsId, Guid attachmentId)
    {
        try
        {
            var attachment = await _context.NewsAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.NewsId == newsId);

            if (attachment == null)
            {
                return ApiResponse.CreateError("Attachment not found");
            }

            await _fileStorage.DeleteAsync(attachment.Url);
            _context.NewsAttachments.Remove(attachment);

            var newsItem = await _context.News.FindAsync(newsId);
            if (newsItem != null)
            {
                newsItem.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ApiResponse.CreateSuccess("Attachment deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to delete attachment",
                new List<string> { ex.Message });
        }
    }

    private static NewsDto MapToDto(News newsItem)
    {
        var attachments = (newsItem.Attachments ?? Enumerable.Empty<NewsAttachment>())
            .OrderBy(a => a.CreatedAt)
            .Select(MapAttachmentToDto)
            .ToList();

        return new NewsDto(
            newsItem.Id,
            newsItem.Title,
            newsItem.Content,
            newsItem.Excerpt,
            newsItem.ImageUrl,
            NormalizeImagePosition(newsItem.ImagePosition),
            newsItem.Author,
            newsItem.Category,
            newsItem.PublishedDate,
            newsItem.IsPinned,
            newsItem.Status,
            newsItem.CreatedAt,
            newsItem.UpdatedAt,
            newsItem.TitleEn,
            newsItem.ContentEn,
            newsItem.ExcerptEn,
            attachments
        );
    }

    private static NewsAttachmentDto MapAttachmentToDto(NewsAttachment attachment) =>
        new(
            attachment.Id,
            attachment.FileName,
            attachment.Url,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.CreatedAt);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeImagePosition(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "top" => "top",
            "bottom" => "bottom",
            _ => "center",
        };
}
