using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class StatisticService : IStatisticService
{
    private readonly ApplicationDbContext _context;

    public StatisticService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<StatisticDto>>> GetAllAsync()
    {
        try
        {
            var statistics = await _context.Statistics
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            var statisticDtos = statistics.Select(MapToDto).ToList();
            return ApiResponse<List<StatisticDto>>.SuccessResponse(statisticDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<StatisticDto>>.ErrorResponse(
                "Failed to retrieve statistics",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<StatisticDto>> UpdateAsync(string key, string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return ApiResponse<StatisticDto>.ErrorResponse("Statistic key is required");
            }

            var statistic = await _context.Statistics.FirstOrDefaultAsync(s => s.Key == key);
            if (statistic == null)
            {
                var metadata = key switch
                {
                    "provinces" => ("Provinces et territoires", 1),
                    "zones" => ("Zones de représentation", 2),
                    "associations" => ("Associations répertoriées", 3),
                    "membership" => ("Adhésion gratuite", 4),
                    _ => (key, 99)
                };
                statistic = new Statistic
                {
                    Key = key,
                    Value = value,
                    Label = metadata.Item1,
                    DisplayOrder = metadata.Item2
                };
                _context.Statistics.Add(statistic);
                await _context.SaveChangesAsync();
                return ApiResponse<StatisticDto>.SuccessResponse(MapToDto(statistic));
            }

            statistic.Value = value;
            statistic.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<StatisticDto>.SuccessResponse(MapToDto(statistic));
        }
        catch (Exception ex)
        {
            return ApiResponse<StatisticDto>.ErrorResponse(
                "Failed to update statistic",
                new List<string> { ex.Message });
        }
    }

    private static StatisticDto MapToDto(Statistic statistic)
    {
        return new StatisticDto(
            statistic.Id,
            statistic.Key,
            statistic.Value,
            statistic.Label,
            statistic.DisplayOrder,
            statistic.CreatedAt,
            statistic.UpdatedAt
        );
    }
}

