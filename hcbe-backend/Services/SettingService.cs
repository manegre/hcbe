using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class SettingService : ISettingService
{
    private readonly ApplicationDbContext _context;

    public SettingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<SiteSettingDto>>> GetAllAsync()
    {
        try
        {
            var settings = await _context.SiteSettings.ToListAsync();
            var settingDtos = settings.Select(MapToDto).ToList();
            return ApiResponse<List<SiteSettingDto>>.SuccessResponse(settingDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<SiteSettingDto>>.ErrorResponse(
                "Failed to retrieve site settings",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<SiteSettingDto>> UpdateAsync(string key, string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return ApiResponse<SiteSettingDto>.ErrorResponse("Setting key is required");
            }

            var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                return ApiResponse<SiteSettingDto>.ErrorResponse("Setting not found");
            }

            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<SiteSettingDto>.SuccessResponse(MapToDto(setting));
        }
        catch (Exception ex)
        {
            return ApiResponse<SiteSettingDto>.ErrorResponse(
                "Failed to update setting",
                new List<string> { ex.Message });
        }
    }

    private static SiteSettingDto MapToDto(SiteSetting setting)
    {
        return new SiteSettingDto(
            setting.Id,
            setting.Key,
            setting.Value,
            setting.Description,
            setting.CreatedAt,
            setting.UpdatedAt
        );
    }
}

