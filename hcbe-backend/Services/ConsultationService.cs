using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class ConsultationService : IConsultationService
{
    private readonly ApplicationDbContext _context;

    public ConsultationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<ConsultationDto>>> GetActiveAsync()
    {
        try
        {
            var items = await _context.Consultations
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Title)
                .ToListAsync();

            return ApiResponse<List<ConsultationDto>>.SuccessResponse(items.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<ConsultationDto>>.ErrorResponse(
                "Failed to retrieve consultations",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<ConsultationDto>>> GetAllForAdminAsync()
    {
        try
        {
            var items = await _context.Consultations
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Title)
                .ToListAsync();

            return ApiResponse<List<ConsultationDto>>.SuccessResponse(items.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<ConsultationDto>>.ErrorResponse(
                "Failed to retrieve consultations",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<ConsultationDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var item = await _context.Consultations
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (item == null)
            {
                return ApiResponse<ConsultationDto>.ErrorResponse("Consultation not found");
            }

            return ApiResponse<ConsultationDto>.SuccessResponse(MapToDto(item));
        }
        catch (Exception ex)
        {
            return ApiResponse<ConsultationDto>.ErrorResponse(
                "Failed to retrieve consultation",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<ConsultationDto>> GetByIdForAdminAsync(Guid id)
    {
        try
        {
            var item = await _context.Consultations.FindAsync(id);
            if (item == null)
            {
                return ApiResponse<ConsultationDto>.ErrorResponse("Consultation not found");
            }

            return ApiResponse<ConsultationDto>.SuccessResponse(MapToDto(item));
        }
        catch (Exception ex)
        {
            return ApiResponse<ConsultationDto>.ErrorResponse(
                "Failed to retrieve consultation",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<ConsultationDto>> CreateAsync(CreateConsultationRequest request)
    {
        try
        {
            var item = new Consultation
            {
                Title = request.Title.Trim(),
                TitleEn = NormalizeOptional(request.TitleEn),
                Description = request.Description.Trim(),
                DescriptionEn = NormalizeOptional(request.DescriptionEn),
                Icon = string.IsNullOrWhiteSpace(request.Icon) ? "ri-chat-poll-line" : request.Icon.Trim(),
                LayoutType = NormalizeLayoutType(request.LayoutType),
                ActionUrl = request.ActionUrl?.Trim(),
                ActionLabel = request.ActionLabel?.Trim(),
                ActionLabelEn = NormalizeOptional(request.ActionLabelEn),
                SecondaryActionUrl = request.SecondaryActionUrl?.Trim(),
                SecondaryActionLabel = request.SecondaryActionLabel?.Trim(),
                SecondaryActionLabelEn = NormalizeOptional(request.SecondaryActionLabelEn),
                AccentColor = NormalizeAccentColor(request.AccentColor),
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.Consultations.Add(item);
            await _context.SaveChangesAsync();

            return ApiResponse<ConsultationDto>.SuccessResponse(MapToDto(item));
        }
        catch (Exception ex)
        {
            return ApiResponse<ConsultationDto>.ErrorResponse(
                "Failed to create consultation",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<ConsultationDto>> UpdateAsync(Guid id, UpdateConsultationRequest request)
    {
        try
        {
            var item = await _context.Consultations.FindAsync(id);
            if (item == null)
            {
                return ApiResponse<ConsultationDto>.ErrorResponse("Consultation not found");
            }

            if (request.Title != null) item.Title = request.Title.Trim();
            if (request.TitleEn != null) item.TitleEn = NormalizeOptional(request.TitleEn);
            if (request.Description != null) item.Description = request.Description.Trim();
            if (request.DescriptionEn != null) item.DescriptionEn = NormalizeOptional(request.DescriptionEn);
            if (request.Icon != null) item.Icon = request.Icon.Trim();
            if (request.LayoutType != null) item.LayoutType = NormalizeLayoutType(request.LayoutType);
            if (request.ActionUrl != null) item.ActionUrl = string.IsNullOrWhiteSpace(request.ActionUrl) ? null : request.ActionUrl.Trim();
            if (request.ActionLabel != null) item.ActionLabel = string.IsNullOrWhiteSpace(request.ActionLabel) ? null : request.ActionLabel.Trim();
            if (request.ActionLabelEn != null) item.ActionLabelEn = NormalizeOptional(request.ActionLabelEn);
            if (request.SecondaryActionUrl != null) item.SecondaryActionUrl = string.IsNullOrWhiteSpace(request.SecondaryActionUrl) ? null : request.SecondaryActionUrl.Trim();
            if (request.SecondaryActionLabel != null) item.SecondaryActionLabel = string.IsNullOrWhiteSpace(request.SecondaryActionLabel) ? null : request.SecondaryActionLabel.Trim();
            if (request.SecondaryActionLabelEn != null) item.SecondaryActionLabelEn = NormalizeOptional(request.SecondaryActionLabelEn);
            if (request.AccentColor != null) item.AccentColor = NormalizeAccentColor(request.AccentColor);
            if (request.DisplayOrder.HasValue) item.DisplayOrder = request.DisplayOrder.Value;
            if (request.IsActive.HasValue) item.IsActive = request.IsActive.Value;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<ConsultationDto>.SuccessResponse(MapToDto(item));
        }
        catch (Exception ex)
        {
            return ApiResponse<ConsultationDto>.ErrorResponse(
                "Failed to update consultation",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        try
        {
            var item = await _context.Consultations.FindAsync(id);
            if (item == null)
            {
                return ApiResponse<bool>.ErrorResponse("Consultation not found");
            }

            _context.Consultations.Remove(item);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Failed to delete consultation",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<bool>> ToggleStatusAsync(Guid id)
    {
        try
        {
            var item = await _context.Consultations.FindAsync(id);
            if (item == null)
            {
                return ApiResponse<bool>.ErrorResponse("Consultation not found");
            }

            item.IsActive = !item.IsActive;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Failed to toggle consultation status",
                new List<string> { ex.Message });
        }
    }

    private static string NormalizeLayoutType(string? layoutType) =>
        layoutType?.Trim().ToLowerInvariant() == "featured" ? "featured" : "card";

    private static string NormalizeAccentColor(string? accentColor) =>
        accentColor?.Trim().ToLowerInvariant() == "amber" ? "amber" : "emerald";

    private static ConsultationDto MapToDto(Consultation item) =>
        new(
            item.Id,
            item.Title,
            item.Description,
            item.Icon,
            item.LayoutType,
            item.ActionUrl,
            item.ActionLabel,
            item.SecondaryActionUrl,
            item.SecondaryActionLabel,
            item.AccentColor,
            item.DisplayOrder,
            item.IsActive,
            item.CreatedAt,
            item.UpdatedAt,
            item.TitleEn,
            item.DescriptionEn,
            item.ActionLabelEn,
            item.SecondaryActionLabelEn
        );

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
