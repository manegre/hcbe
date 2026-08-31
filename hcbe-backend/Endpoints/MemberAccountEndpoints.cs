using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Endpoints;

public static class MemberAccountEndpoints
{
    public static void MapMemberAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/member-account")
            .WithTags("Member account")
            .RequireAuthorization("Authenticated")
            .WithOpenApi();

        group.MapGet("/me", async (HttpContext context, IMemberAccountService service) =>
        {
            var userId = context.GetUserId();
            return userId is null
                ? Results.Unauthorized()
                : (await service.GetAsync(userId.Value)).HandleServiceResponse();
        });

        group.MapPut("/me", async (
            UpdateMemberAccountRequest request,
            HttpContext context,
            IMemberAccountService service) =>
        {
            var userId = context.GetUserId();
            return userId is null
                ? Results.Unauthorized()
                : (await service.UpdateAsync(userId.Value, request)).HandleServiceResponse();
        });
    }
}
