using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class FooterService : IFooterService
{
    private readonly ApplicationDbContext _context;

    public FooterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<FooterLinkDto>>> GetAllAsync(bool includeInactive = false)
    {
        try
        {
            var query = _context.FooterLinks.AsQueryable();
            if (!includeInactive) query = query.Where(item => item.IsActive);
            var links = await query
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();

            var linkDtos = links.Select(MapToDto).ToList();
            return ApiResponse<List<FooterLinkDto>>.SuccessResponse(linkDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<FooterLinkDto>>.ErrorResponse(
                "Failed to retrieve footer links",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<FooterLinkDto>> CreateAsync(CreateFooterLinkRequest request)
    {
        try
        {
            var link = new FooterLink
            {
                Category = request.Category,
                CategoryEn = Normalize(request.CategoryEn),
                Label = request.Label,
                LabelEn = Normalize(request.LabelEn),
                Url = request.Url,
                IsActive = request.IsActive,
                DisplayOrder = request.DisplayOrder
            };

            _context.FooterLinks.Add(link);
            await _context.SaveChangesAsync();

            return ApiResponse<FooterLinkDto>.SuccessResponse(MapToDto(link));
        }
        catch (Exception ex)
        {
            return ApiResponse<FooterLinkDto>.ErrorResponse(
                "Failed to create footer link",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<FooterLinkDto>> UpdateAsync(Guid id, UpdateFooterLinkRequest request)
    {
        try
        {
            var link = await _context.FooterLinks.FindAsync(id);
            if (link == null)
            {
                return ApiResponse<FooterLinkDto>.ErrorResponse("Footer link not found");
            }

            if (request.Category != null) link.Category = request.Category;
            if (request.CategoryEn != null) link.CategoryEn = Normalize(request.CategoryEn);
            if (request.Label != null) link.Label = request.Label;
            if (request.LabelEn != null) link.LabelEn = Normalize(request.LabelEn);
            if (request.Url != null) link.Url = request.Url;
            if (request.IsActive.HasValue) link.IsActive = request.IsActive.Value;
            if (request.DisplayOrder.HasValue) link.DisplayOrder = request.DisplayOrder.Value;
            link.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<FooterLinkDto>.SuccessResponse(MapToDto(link));
        }
        catch (Exception ex)
        {
            return ApiResponse<FooterLinkDto>.ErrorResponse(
                "Failed to update footer link",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        try
        {
            var link = await _context.FooterLinks.FindAsync(id);
            if (link == null)
            {
                return ApiResponse.CreateError("Footer link not found");
            }

            _context.FooterLinks.Remove(link);
            await _context.SaveChangesAsync();

            return ApiResponse.CreateSuccess("Footer link deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.CreateError(
                "Failed to delete footer link",
                new List<string> { ex.Message });
        }
    }

    private static FooterLinkDto MapToDto(FooterLink link)
    {
        return new FooterLinkDto(
            link.Id,
            link.Category,
            link.Label,
            link.Url,
            link.IsActive,
            link.DisplayOrder,
            link.CreatedAt,
            link.UpdatedAt,
            link.CategoryEn,
            link.LabelEn
        );
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

