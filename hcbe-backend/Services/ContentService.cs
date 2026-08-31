using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class ContentService : IContentService
{
    private readonly ApplicationDbContext _context;

    public ContentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<PageSectionDto>>> GetPageSectionsAsync(string? page, bool includeInactive = false)
    {
        try
        {
            var query = _context.PageSections.AsQueryable();
            if (!includeInactive) query = query.Where(ps => ps.IsActive);
            if (!string.IsNullOrWhiteSpace(page))
            {
                query = query.Where(ps => ps.Page == page);
            }

            var sections = await query
                .OrderBy(ps => ps.DisplayOrder)
                .ToListAsync();

            var sectionDtos = sections.Select(MapToDto).ToList();
            return ApiResponse<List<PageSectionDto>>.SuccessResponse(sectionDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<PageSectionDto>>.ErrorResponse(
                "Failed to retrieve page sections",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<ServiceContentDto>>> GetServicesAsync(bool includeInactive = false)
    {
        try
        {
            var query = _context.ServiceContents.AsQueryable();
            if (!includeInactive) query = query.Where(s => s.IsActive);
            var services = await query
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            var serviceDtos = services.Select(MapToDto).ToList();
            return ApiResponse<List<ServiceContentDto>>.SuccessResponse(serviceDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<ServiceContentDto>>.ErrorResponse(
                "Failed to retrieve services",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<PageSectionDto>> CreatePageSectionAsync(CreatePageSectionRequest request)
    {
        var exists = await _context.PageSections.AnyAsync(item => item.Page == request.Page && item.Section == request.Section);
        if (exists) return ApiResponse<PageSectionDto>.ErrorResponse("This page section already exists");
        var section = new PageSection
        {
            Page = request.Page.Trim().ToLowerInvariant(),
            Section = request.Section.Trim().ToLowerInvariant(),
            Title = Normalize(request.Title),
            TitleEn = Normalize(request.TitleEn),
            Content = Normalize(request.Content),
            ContentEn = Normalize(request.ContentEn),
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };
        _context.PageSections.Add(section);
        await _context.SaveChangesAsync();
        return ApiResponse<PageSectionDto>.SuccessResponse(MapToDto(section));
    }

    public async Task<ApiResponse<PageSectionDto>> UpdatePageSectionAsync(Guid id, UpdatePageSectionRequest request)
    {
        try
        {
            var section = await _context.PageSections.FindAsync(id);
            if (section == null)
            {
                return ApiResponse<PageSectionDto>.ErrorResponse("Page section not found");
            }

            // Allow setting Title to null/empty to clear it
            if (request.Title != null)
            {
                section.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title;
            }
            if (request.Content != null) section.Content = request.Content;
            if (request.TitleEn != null) section.TitleEn = Normalize(request.TitleEn);
            if (request.ContentEn != null) section.ContentEn = Normalize(request.ContentEn);
            if (request.IsActive.HasValue) section.IsActive = request.IsActive.Value;
            if (request.DisplayOrder.HasValue) section.DisplayOrder = request.DisplayOrder.Value;
            section.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<PageSectionDto>.SuccessResponse(MapToDto(section));
        }
        catch (Exception ex)
        {
            return ApiResponse<PageSectionDto>.ErrorResponse(
                "Failed to update page section",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeletePageSectionAsync(Guid id)
    {
        var section = await _context.PageSections.FindAsync(id);
        if (section is null) return ApiResponse.CreateError("Page section not found");
        _context.PageSections.Remove(section);
        await _context.SaveChangesAsync();
        return ApiResponse.CreateSuccess("Page section deleted");
    }

    public async Task<ApiResponse<ServiceContentDto>> CreateServiceAsync(CreateServiceContentRequest request)
    {
        var service = new ServiceContent
        {
            Title = request.Title.Trim(), TitleEn = Normalize(request.TitleEn),
            Description = Normalize(request.Description), DescriptionEn = Normalize(request.DescriptionEn),
            Icon = Normalize(request.Icon), Category = Normalize(request.Category), CategoryEn = Normalize(request.CategoryEn),
            IsActive = request.IsActive, DisplayOrder = request.DisplayOrder,
            Details = Normalize(request.Details), DetailsEn = Normalize(request.DetailsEn),
            ExtendedInfo = Normalize(request.ExtendedInfo), ExtendedInfoEn = Normalize(request.ExtendedInfoEn)
        };
        _context.ServiceContents.Add(service);
        await _context.SaveChangesAsync();
        return ApiResponse<ServiceContentDto>.SuccessResponse(MapToDto(service));
    }

    public async Task<ApiResponse<ServiceContentDto>> UpdateServiceAsync(Guid id, UpdateServiceContentRequest request)
    {
        try
        {
            var service = await _context.ServiceContents.FindAsync(id);
            if (service == null)
            {
                return ApiResponse<ServiceContentDto>.ErrorResponse("Service content not found");
            }

            if (request.Title != null) service.Title = request.Title;
            if (request.Description != null) service.Description = request.Description;
            if (request.TitleEn != null) service.TitleEn = Normalize(request.TitleEn);
            if (request.DescriptionEn != null) service.DescriptionEn = Normalize(request.DescriptionEn);
            if (request.Icon != null) service.Icon = request.Icon;
            if (request.Category != null) service.Category = request.Category;
            if (request.CategoryEn != null) service.CategoryEn = Normalize(request.CategoryEn);
            if (request.IsActive.HasValue) service.IsActive = request.IsActive.Value;
            if (request.Details != null) service.Details = request.Details;
            if (request.DetailsEn != null) service.DetailsEn = Normalize(request.DetailsEn);
            if (request.ExtendedInfo != null) service.ExtendedInfo = request.ExtendedInfo;
            if (request.ExtendedInfoEn != null) service.ExtendedInfoEn = Normalize(request.ExtendedInfoEn);
            if (request.DisplayOrder.HasValue) service.DisplayOrder = request.DisplayOrder.Value;
            service.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<ServiceContentDto>.SuccessResponse(MapToDto(service));
        }
        catch (Exception ex)
        {
            return ApiResponse<ServiceContentDto>.ErrorResponse(
                "Failed to update service content",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> DeleteServiceAsync(Guid id)
    {
        var service = await _context.ServiceContents.FindAsync(id);
        if (service is null) return ApiResponse.CreateError("Service content not found");
        _context.ServiceContents.Remove(service);
        await _context.SaveChangesAsync();
        return ApiResponse.CreateSuccess("Service content deleted");
    }

    private static PageSectionDto MapToDto(PageSection section)
    {
        return new PageSectionDto(
            section.Id,
            section.Page,
            section.Section,
            section.Title,
            section.Content,
            section.IsActive,
            section.DisplayOrder,
            section.CreatedAt,
            section.UpdatedAt,
            section.TitleEn,
            section.ContentEn
        );
    }

    private static ServiceContentDto MapToDto(ServiceContent service)
    {
        return new ServiceContentDto(
            service.Id,
            service.Title,
            service.Description,
            service.Icon,
            service.Category,
            service.IsActive,
            service.DisplayOrder,
            service.Details,
            service.ExtendedInfo,
            service.CreatedAt,
            service.UpdatedAt,
            service.TitleEn,
            service.DescriptionEn,
            service.CategoryEn,
            service.DetailsEn,
            service.ExtendedInfoEn
        );
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

