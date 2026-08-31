using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class GrantService : IGrantService
{
    private readonly ApplicationDbContext _context;

    public GrantService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<GrantProgramDto>>> GetActiveAsync()
    {
        try
        {
            var grants = await _context.GrantPrograms
                .Where(g => g.IsActive)
                .OrderBy(g => g.DisplayOrder)
                .ThenBy(g => g.Title)
                .ToListAsync();

            return ApiResponse<List<GrantProgramDto>>.SuccessResponse(grants.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<GrantProgramDto>>.ErrorResponse(
                "Failed to retrieve grant programs",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<GrantProgramDto>>> GetAllForAdminAsync()
    {
        try
        {
            var grants = await _context.GrantPrograms
                .OrderBy(g => g.DisplayOrder)
                .ThenBy(g => g.Title)
                .ToListAsync();

            return ApiResponse<List<GrantProgramDto>>.SuccessResponse(grants.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<GrantProgramDto>>.ErrorResponse(
                "Failed to retrieve grant programs",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<GrantProgramDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var grant = await _context.GrantPrograms
                .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);

            if (grant == null)
            {
                return ApiResponse<GrantProgramDto>.ErrorResponse("Grant program not found");
            }

            return ApiResponse<GrantProgramDto>.SuccessResponse(MapToDto(grant));
        }
        catch (Exception ex)
        {
            return ApiResponse<GrantProgramDto>.ErrorResponse(
                "Failed to retrieve grant program",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<GrantProgramDto>> GetByIdForAdminAsync(Guid id)
    {
        try
        {
            var grant = await _context.GrantPrograms.FindAsync(id);
            if (grant == null)
            {
                return ApiResponse<GrantProgramDto>.ErrorResponse("Grant program not found");
            }

            return ApiResponse<GrantProgramDto>.SuccessResponse(MapToDto(grant));
        }
        catch (Exception ex)
        {
            return ApiResponse<GrantProgramDto>.ErrorResponse(
                "Failed to retrieve grant program",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<GrantProgramDto>> CreateAsync(CreateGrantProgramRequest request)
    {
        try
        {
            var grant = new GrantProgram
            {
                Title = request.Title.Trim(),
                TitleEn = NormalizeOptional(request.TitleEn),
                Description = request.Description.Trim(),
                DescriptionEn = NormalizeOptional(request.DescriptionEn),
                Icon = string.IsNullOrWhiteSpace(request.Icon) ? "ri-graduation-cap-line" : request.Icon.Trim(),
                Amount = request.Amount.Trim(),
                AmountEn = NormalizeOptional(request.AmountEn),
                Duration = request.Duration.Trim(),
                DurationEn = NormalizeOptional(request.DurationEn),
                EligibilityCriteria = request.EligibilityCriteria ?? new List<string>(),
                EligibilityCriteriaEn = request.EligibilityCriteriaEn ?? new List<string>(),
                ApplicationUrl = request.ApplicationUrl?.Trim(),
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.GrantPrograms.Add(grant);
            await _context.SaveChangesAsync();

            return ApiResponse<GrantProgramDto>.SuccessResponse(MapToDto(grant));
        }
        catch (Exception ex)
        {
            return ApiResponse<GrantProgramDto>.ErrorResponse(
                "Failed to create grant program",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<GrantProgramDto>> UpdateAsync(Guid id, UpdateGrantProgramRequest request)
    {
        try
        {
            var grant = await _context.GrantPrograms.FindAsync(id);
            if (grant == null)
            {
                return ApiResponse<GrantProgramDto>.ErrorResponse("Grant program not found");
            }

            if (request.Title != null) grant.Title = request.Title.Trim();
            if (request.TitleEn != null) grant.TitleEn = NormalizeOptional(request.TitleEn);
            if (request.Description != null) grant.Description = request.Description.Trim();
            if (request.DescriptionEn != null) grant.DescriptionEn = NormalizeOptional(request.DescriptionEn);
            if (request.Icon != null) grant.Icon = request.Icon.Trim();
            if (request.Amount != null) grant.Amount = request.Amount.Trim();
            if (request.AmountEn != null) grant.AmountEn = NormalizeOptional(request.AmountEn);
            if (request.Duration != null) grant.Duration = request.Duration.Trim();
            if (request.DurationEn != null) grant.DurationEn = NormalizeOptional(request.DurationEn);
            if (request.EligibilityCriteria != null) grant.EligibilityCriteria = request.EligibilityCriteria;
            if (request.EligibilityCriteriaEn != null) grant.EligibilityCriteriaEn = request.EligibilityCriteriaEn;
            if (request.ApplicationUrl != null) grant.ApplicationUrl = string.IsNullOrWhiteSpace(request.ApplicationUrl) ? null : request.ApplicationUrl.Trim();
            if (request.DisplayOrder.HasValue) grant.DisplayOrder = request.DisplayOrder.Value;
            if (request.IsActive.HasValue) grant.IsActive = request.IsActive.Value;
            grant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<GrantProgramDto>.SuccessResponse(MapToDto(grant));
        }
        catch (Exception ex)
        {
            return ApiResponse<GrantProgramDto>.ErrorResponse(
                "Failed to update grant program",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        try
        {
            var grant = await _context.GrantPrograms.FindAsync(id);
            if (grant == null)
            {
                return ApiResponse<bool>.ErrorResponse("Grant program not found");
            }

            _context.GrantPrograms.Remove(grant);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Failed to delete grant program",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<bool>> ToggleStatusAsync(Guid id)
    {
        try
        {
            var grant = await _context.GrantPrograms.FindAsync(id);
            if (grant == null)
            {
                return ApiResponse<bool>.ErrorResponse("Grant program not found");
            }

            grant.IsActive = !grant.IsActive;
            grant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Failed to toggle grant program status",
                new List<string> { ex.Message });
        }
    }

    private static GrantProgramDto MapToDto(GrantProgram grant)
    {
        return new GrantProgramDto(
            grant.Id,
            grant.Title,
            grant.Description,
            grant.Icon,
            grant.Amount,
            grant.Duration,
            grant.EligibilityCriteria ?? new List<string>(),
            grant.ApplicationUrl,
            grant.DisplayOrder,
            grant.IsActive,
            grant.CreatedAt,
            grant.UpdatedAt,
            grant.TitleEn,
            grant.DescriptionEn,
            grant.AmountEn,
            grant.DurationEn,
            grant.EligibilityCriteriaEn ?? new List<string>()
        );
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
