using System.Text.Json;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public class PublicSubmissionService : IPublicSubmissionService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "contact", "volunteer", "event-registration", "grant-application",
        "consultation-response", "project-contribution"
    };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "InReview", "Resolved", "Rejected"
    };

    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notifications;

    public PublicSubmissionService(ApplicationDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task<ApiResponse<PublicSubmissionDto>> SubmitAsync(CreatePublicSubmissionRequest request)
    {
        var type = request.Type.Trim().ToLowerInvariant();
        if (!AllowedTypes.Contains(type))
        {
            return ApiResponse<PublicSubmissionDto>.ErrorResponse("Unsupported submission type");
        }

        var entity = new PublicSubmission
        {
            Type = type,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = Normalize(request.Phone),
            Subject = Normalize(request.Subject),
            City = Normalize(request.City),
            Details = request.Details.Trim(),
            MetadataJson = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata)
        };

        _context.PublicSubmissions.Add(entity);
        await _context.SaveChangesAsync();
        await _notifications.CreateNotificationAsync(
            "submission",
            type == "volunteer" ? "Nouvelle candidature bénévole" : "Nouveau message public",
            $"{entity.FirstName} {entity.LastName} — {entity.Subject ?? entity.Type}",
            entity.Id,
            "/admin/submissions");

        return ApiResponse<PublicSubmissionDto>.SuccessResponse(Map(entity), "Submission received");
    }

    public async Task<ApiResponse<List<PublicSubmissionDto>>> GetAllAsync(string? type, string? status)
    {
        var query = _context.PublicSubmissions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim().ToLowerInvariant();
            query = query.Where(item => item.Type == normalizedType);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        var items = await query.OrderByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<PublicSubmissionDto>>.SuccessResponse(items.Select(Map).ToList());
    }

    public async Task<ApiResponse<PagedResult<PublicSubmissionDto>>> SearchAsync(
        int page, int pageSize, string? search, string? sort, string? type, string? status)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _context.PublicSubmissions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim().ToLowerInvariant();
            query = query.Where(item => item.Type == normalizedType);
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item =>
                item.FirstName.ToLower().Contains(term) || item.LastName.ToLower().Contains(term) ||
                item.Email.ToLower().Contains(term) ||
                (item.Subject != null && item.Subject.ToLower().Contains(term)) ||
                item.Details.ToLower().Contains(term));
        }

        query = sort?.ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(item => item.CreatedAt),
            "name" => query.OrderBy(item => item.LastName).ThenBy(item => item.FirstName),
            _ => query.OrderByDescending(item => item.CreatedAt)
        };
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return ApiResponse<PagedResult<PublicSubmissionDto>>.SuccessResponse(
            PagedResult<PublicSubmissionDto>.Create(items.Select(Map).ToList(), page, pageSize, total));
    }

    public async Task<ApiResponse<PublicSubmissionDto>> GetByIdAsync(Guid id)
    {
        var item = await _context.PublicSubmissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null
            ? ApiResponse<PublicSubmissionDto>.ErrorResponse("Submission not found")
            : ApiResponse<PublicSubmissionDto>.SuccessResponse(Map(item));
    }

    public async Task<ApiResponse<PublicSubmissionDto>> UpdateStatusAsync(
        Guid id,
        UpdatePublicSubmissionStatusRequest request)
    {
        if (!AllowedStatuses.Contains(request.Status))
        {
            return ApiResponse<PublicSubmissionDto>.ErrorResponse("Unsupported submission status");
        }

        var item = await _context.PublicSubmissions.FindAsync(id);
        if (item is null)
        {
            return ApiResponse<PublicSubmissionDto>.ErrorResponse("Submission not found");
        }

        item.Status = request.Status;
        item.ReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse<PublicSubmissionDto>.SuccessResponse(Map(item));
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        var item = await _context.PublicSubmissions.FindAsync(id);
        if (item is null)
        {
            return ApiResponse.CreateError("Submission not found");
        }

        _context.PublicSubmissions.Remove(item);
        await _context.SaveChangesAsync();
        return ApiResponse.CreateSuccess("Submission deleted");
    }

    private static PublicSubmissionDto Map(PublicSubmission item) => new(
        item.Id, item.Type, item.FirstName, item.LastName, item.Email, item.Phone,
        item.Subject, item.City, item.Details, item.MetadataJson, item.Status,
        item.CreatedAt, item.ReviewedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
