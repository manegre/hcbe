using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class UserAdminService : IUserAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailOutbox _emailOutbox;
    private readonly IEmailTemplateRenderer _emailTemplates;
    private readonly IConfiguration _configuration;

    public UserAdminService(
        ApplicationDbContext context,
        IEmailOutbox emailOutbox,
        IEmailTemplateRenderer emailTemplates,
        IConfiguration configuration)
    {
        _context = context;
        _emailOutbox = emailOutbox;
        _emailTemplates = emailTemplates;
        _configuration = configuration;
    }

    public async Task<ApiResponse<List<AdminUserDto>>> GetAdminUsersAsync()
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.IsAdmin)
                .OrderBy(u => u.Email)
                .ToListAsync();

            return ApiResponse<List<AdminUserDto>>.SuccessResponse(users.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<AdminUserDto>>.ErrorResponse(
                "Failed to retrieve admin users",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<AdminUserDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsAdmin)
            {
                return ApiResponse<AdminUserDto>.ErrorResponse("Admin user not found");
            }

            return ApiResponse<AdminUserDto>.SuccessResponse(MapToDto(user));
        }
        catch (Exception ex)
        {
            return ApiResponse<AdminUserDto>.ErrorResponse(
                "Failed to retrieve admin user",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<AdminUserDto>> CreateAdminUserAsync(CreateAdminUserRequest request)
    {
        try
        {
            if (!PasswordPolicy.IsStrong(request.Password))
            {
                return ApiResponse<AdminUserDto>.ErrorResponse(PasswordPolicy.ValidationMessage);
            }

            var email = request.Email.Trim().ToLowerInvariant();
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
            {
                return ApiResponse<AdminUserDto>.ErrorResponse("A user with this email already exists");
            }

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName?.Trim(),
                LastName = request.LastName?.Trim(),
                IsAdmin = true,
                MustChangePassword = true,
                CreatedAt = DateTime.UtcNow,
            };

            var member = await _context.Members.FirstOrDefaultAsync(item => item.Email.ToLower() == email);
            if (member is null)
            {
                member = new Member
                {
                    Email = email,
                    FirstName = string.IsNullOrWhiteSpace(user.FirstName) ? email.Split('@', 2)[0] : user.FirstName,
                    LastName = user.LastName ?? string.Empty,
                    IsAdmin = true
                };
                _context.Members.Add(member);
            }
            else
            {
                member.IsAdmin = true;
            }

            user.MemberId = member.Id;

            _context.Users.Add(user);
            var publicUrl = (_configuration["PublicAppUrl"] ?? "http://localhost:3000").TrimEnd('/');
            var welcome = _emailTemplates.AdminWelcome(
                user.FirstName,
                user.Email,
                request.Password,
                $"{publicUrl}/admin/login");
            _emailOutbox.Enqueue(user.Email, welcome.Subject, welcome.HtmlBody, nameof(User), user.Id);
            await _context.SaveChangesAsync();

            return ApiResponse<AdminUserDto>.SuccessResponse(MapToDto(user));
        }
        catch (Exception ex)
        {
            return ApiResponse<AdminUserDto>.ErrorResponse(
                "Failed to create admin user",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<AdminUserDto>> UpdateAsync(Guid id, UpdateAdminUserRequest request, Guid currentUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsAdmin)
            {
                return ApiResponse<AdminUserDto>.ErrorResponse("Admin user not found");
            }

            if (request.IsAdmin == false)
            {
                if (id == currentUserId)
                {
                    return ApiResponse<AdminUserDto>.ErrorResponse("You cannot remove your own admin privileges");
                }

                var adminCount = await _context.Users.CountAsync(u => u.IsAdmin);
                if (adminCount <= 1)
                {
                    return ApiResponse<AdminUserDto>.ErrorResponse("Cannot remove the last admin user");
                }

                user.IsAdmin = false;
            }

            if (request.FirstName != null) user.FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim();
            if (request.LastName != null) user.LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim();
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            await _context.SaveChangesAsync();

            return ApiResponse<AdminUserDto>.SuccessResponse(MapToDto(user));
        }
        catch (Exception ex)
        {
            return ApiResponse<AdminUserDto>.ErrorResponse(
                "Failed to update admin user",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid currentUserId)
    {
        try
        {
            if (id == currentUserId)
            {
                return ApiResponse<bool>.ErrorResponse("You cannot delete your own account");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsAdmin)
            {
                return ApiResponse<bool>.ErrorResponse("Admin user not found");
            }

            var adminCount = await _context.Users.CountAsync(u => u.IsAdmin);
            if (adminCount <= 1)
            {
                return ApiResponse<bool>.ErrorResponse("Cannot delete the last admin user");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Failed to delete admin user",
                new List<string> { ex.Message });
        }
    }

    private static AdminUserDto MapToDto(User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.IsAdmin,
            user.MustChangePassword, user.MemberId, user.CreatedAt);
}
