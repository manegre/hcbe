using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IStatisticService
{
    Task<ApiResponse<List<StatisticDto>>> GetAllAsync();
    Task<ApiResponse<StatisticDto>> UpdateAsync(string key, string value);
}

