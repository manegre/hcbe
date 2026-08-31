using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class SettingEndpoints
{
    public static void MapSettingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/settings")
            .WithTags("Settings")
            .WithOpenApi();

        group.MapGet("/", async (ISettingService settingService) =>
        {
            var response = await settingService.GetAllAsync();
            return response.HandleServiceResponse();
        })
        .WithName("GetSiteSettings")
        .Produces<ApiResponse<List<SiteSettingDto>>>()
        .Produces(400);

        group.MapPut("/{key}", async (string key, string value, HttpContext context, ISettingService settingService) =>
        {
            if (!context.IsAdmin())
            {
                return Results.Forbid();
            }

            var response = await settingService.UpdateAsync(key, value);
            return response.HandleServiceResponse();
        })
        .WithName("UpdateSiteSetting")
        .RequireAuthorization()
        .Produces<ApiResponse<SiteSettingDto>>()
        .Produces(403)
        .Produces(404)
        .Produces(400);
    }
}
