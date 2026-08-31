using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class NavigationService : INavigationService
{
    private readonly ApplicationDbContext _context;

    public NavigationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<NavigationItemDto>>> GetAllAsync(bool includeInactive = false)
    {
        try
        {
            var query = _context.NavigationItems.AsQueryable();
            if (!includeInactive) query = query.Where(item => item.IsActive);
            var items = await query
                .OrderBy(n => n.DisplayOrder)
                .ToListAsync();

            var itemDtos = items.Select(MapToDto).ToList();
            return ApiResponse<List<NavigationItemDto>>.SuccessResponse(itemDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<NavigationItemDto>>.ErrorResponse(
                "Failed to retrieve navigation items",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<NavigationItemDto>> CreateAsync(CreateNavigationItemRequest request)
    {
        try
        {
            var item = new NavigationItem
            {
                Label = request.Label,
                LabelEn = Normalize(request.LabelEn),
                Url = request.Url,
                IsActive = request.IsActive,
                DisplayOrder = request.DisplayOrder
            };

            _context.NavigationItems.Add(item);
            await _context.SaveChangesAsync();

            return ApiResponse<NavigationItemDto>.SuccessResponse(MapToDto(item));
        }
        catch (Exception ex)
        {
            return ApiResponse<NavigationItemDto>.ErrorResponse(
                "Failed to create navigation item",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<NavigationItemDto>> UpdateAsync(Guid id, UpdateNavigationItemRequest request)
    {
        try
        {
            var item = await _context.NavigationItems.FindAsync(id);
            if (item == null)
            {
                return ApiResponse<NavigationItemDto>.ErrorResponse("Navigation item not found");
            }

            if (request.Label != null) item.Label = request.Label;
            if (request.LabelEn != null) item.LabelEn = Normalize(request.LabelEn);
            if (request.Url != null) item.Url = request.Url;
            if (request.IsActive.HasValue) item.IsActive = request.IsActive.Value;
            if (request.DisplayOrder.HasValue) item.DisplayOrder = request.DisplayOrder.Value;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<NavigationItemDto>.SuccessResponse(MapToDto(item));
        }
        catch (Exception ex)
        {
            return ApiResponse<NavigationItemDto>.ErrorResponse(
                "Failed to update navigation item",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try
        {
            var item = await _context.NavigationItems.FindAsync(id);
            if (item == null)
            {
                return ApiResponse.CreateError("Navigation item not found");
            }

            _context.NavigationItems.Remove(item);
            await _context.SaveChangesAsync();

            return ApiResponse.CreateSuccess("Navigation item deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to delete navigation item",
                new List<string> { ex.Message });
        }
    }

    private static NavigationItemDto MapToDto(NavigationItem item)
    {
        return new NavigationItemDto(
            item.Id,
            item.Label,
            item.Url,
            item.IsActive,
            item.DisplayOrder,
            item.CreatedAt,
            item.UpdatedAt,
            item.LabelEn
        );
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

