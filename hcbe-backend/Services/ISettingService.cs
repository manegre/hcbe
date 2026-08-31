using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface ISettingService
{
    Task<ApiResponse<List<SiteSettingDto>>> GetAllAsync();
    Task<ApiResponse<SiteSettingDto>> UpdateAsync(string key, string value);
}

