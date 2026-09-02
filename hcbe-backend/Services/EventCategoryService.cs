using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed partial class EventCategoryService : IEventCategoryService
{
    private readonly ApplicationDbContext _context;

    public EventCategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<EventCategoryDto>>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.EventCategories.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(category => category.IsActive);
        }

        var categories = await query
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => MapToDto(category))
            .ToListAsync();

        return ApiResponse<List<EventCategoryDto>>.SuccessResponse(categories);
    }

    public async Task<ApiResponse<EventCategoryDto>> CreateAsync(CreateEventCategoryRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            return ApiResponse<EventCategoryDto>.ErrorResponse("Category name is required");
        }

        var slug = Slugify(string.IsNullOrWhiteSpace(request.Slug) ? name : request.Slug);
        if (slug.Length == 0)
        {
            return ApiResponse<EventCategoryDto>.ErrorResponse("Category slug is invalid");
        }

        if (await _context.EventCategories.AnyAsync(category => category.Slug == slug))
        {
            return ApiResponse<EventCategoryDto>.ErrorResponse("A category with this slug already exists");
        }

        var category = new EventCategory
        {
            Name = name,
            NameEn = Normalize(request.NameEn),
            Slug = slug,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };

        _context.EventCategories.Add(category);
        await _context.SaveChangesAsync();
        return ApiResponse<EventCategoryDto>.SuccessResponse(MapToDto(category));
    }

    public async Task<ApiResponse<EventCategoryDto>> UpdateAsync(Guid id, UpdateEventCategoryRequest request)
    {
        var category = await _context.EventCategories.FindAsync(id);
        if (category is null)
        {
            return ApiResponse<EventCategoryDto>.ErrorResponse("Event category not found");
        }

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (name.Length == 0)
            {
                return ApiResponse<EventCategoryDto>.ErrorResponse("Category name is required");
            }
            category.Name = name;
        }

        if (request.NameEn is not null) category.NameEn = Normalize(request.NameEn);
        if (request.IsActive.HasValue) category.IsActive = request.IsActive.Value;
        if (request.DisplayOrder.HasValue) category.DisplayOrder = request.DisplayOrder.Value;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse<EventCategoryDto>.SuccessResponse(MapToDto(category));
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        var category = await _context.EventCategories.FindAsync(id);
        if (category is null)
        {
            return ApiResponse.CreateError("Event category not found");
        }

        var isUsed = await _context.Events.AnyAsync(item => item.Type != null && item.Type.ToLower() == category.Slug);
        if (isUsed)
        {
            return ApiResponse.CreateError("This category is used by one or more events. Deactivate it instead.");
        }

        _context.EventCategories.Remove(category);
        await _context.SaveChangesAsync();
        return ApiResponse.CreateSuccess("Event category deleted");
    }

    private static EventCategoryDto MapToDto(EventCategory category) => new(
        category.Id,
        category.Slug,
        category.Name,
        category.NameEn,
        category.IsActive,
        category.DisplayOrder,
        category.CreatedAt,
        category.UpdatedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return SlugSeparatorRegex().Replace(builder.ToString().Normalize(NormalizationForm.FormC), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex SlugSeparatorRegex();
}
