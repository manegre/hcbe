using System.Security.Claims;
using HcbeApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HcbeApi.Helpers;

public static class HttpContextExtensions
{
    public static Guid? GetUserId(this HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Admin check uses the database as source of truth so a JWT issued before
    /// a user was promoted still authorizes admin endpoints.
    /// </summary>
    public static bool IsAdmin(this HttpContext context)
    {
        var userId = context.GetUserId();
        if (userId == null)
        {
            return false;
        }

        var db = context.RequestServices.GetService<ApplicationDbContext>();
        if (db == null)
        {
            return context.User.FindFirst("isAdmin")?.Value?.ToLowerInvariant() == "true";
        }

        return db.Users.AsNoTracking().Any(u => u.Id == userId && u.IsAdmin && !u.MustChangePassword);
    }

    public static IResult ForbidIfNotAdmin(this HttpContext context)
    {
        return context.IsAdmin() ? Results.Ok() : Results.Forbid();
    }
}
