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

    public static bool HasPermission(this HttpContext context, string permission)
    {
        var userId = context.GetUserId();
        if (userId == null) return false;

        var db = context.RequestServices.GetService<ApplicationDbContext>();
        if (db == null)
        {
            var isAdmin = string.Equals(context.User.FindFirst("isAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase);
            var isSuperAdmin = string.Equals(context.User.FindFirst("adminRole")?.Value, AdminAccess.SuperAdmin, StringComparison.OrdinalIgnoreCase);
            return isAdmin && (isSuperAdmin || context.User.FindAll("permission").Any(claim => claim.Value == permission));
        }

        var admin = db.Users.AsNoTracking()
            .Where(user => user.Id == userId && user.IsAdmin && user.IsActive && !user.MustChangePassword)
            .Select(user => new { user.AdminRole, user.AdminPermissions })
            .SingleOrDefault();
        return admin is not null && AdminAccess.EffectivePermissions(admin.AdminRole, admin.AdminPermissions).Contains(permission);
    }

    public static IResult ForbidIfNotAdmin(this HttpContext context)
    {
        return context.IsAdmin() ? Results.Ok() : Results.Forbid();
    }
}
