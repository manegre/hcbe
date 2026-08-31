using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class StatisticEndpoints
{
    public static void MapStatisticEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/statistics")
            .WithTags("Statistics")
            .WithOpenApi();

        group.MapGet("/", async (IStatisticService statisticService) =>
        {
            var response = await statisticService.GetAllAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetStatistics")
        .Produces<ApiResponse<List<StatisticDto>>>()
        .Produces(400);

        group.MapPut("/{key}", async (string key, string value, HttpContext context, IStatisticService statisticService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await statisticService.UpdateAsync(key, value);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateStatistic")
        .RequireAuthorization()
        .Produces<ApiResponse<StatisticDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
