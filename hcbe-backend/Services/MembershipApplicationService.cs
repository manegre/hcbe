using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class MembershipApplicationService : IMembershipApplicationService
{
    private readonly ApplicationDbContext _context;

    public MembershipApplicationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<MembershipApplicationDto>> SubmitAsync(CreateMembershipApplicationRequest request)
    {
        try
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var existingMember = await _context.Members
                .AnyAsync(m => m.Email.ToLower() == normalizedEmail);
            if (existingMember)
            {
                return ApiResponse<MembershipApplicationDto>.ErrorResponse(
                    "A member with this email already exists");
            }

            var pendingApplication = await _context.MembershipApplications
                .AnyAsync(a => a.Email.ToLower() == normalizedEmail
                    && a.Status == MembershipApplicationStatus.Pending);
            if (pendingApplication)
            {
                return ApiResponse<MembershipApplicationDto>.ErrorResponse(
                    "A pending application already exists for this email");
            }

            var application = new MembershipApplication
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone?.Trim(),
                City = request.City?.Trim(),
                Province = request.Province?.Trim(),
                Profession = request.Profession?.Trim(),
                Expertise = request.Expertise?.Trim(),
                Motivation = request.Motivation?.Trim(),
                PasswordHash = string.IsNullOrWhiteSpace(request.Password)
                    ? null
                    : BCrypt.Net.BCrypt.HashPassword(request.Password),
                Status = MembershipApplicationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.MembershipApplications.Add(application);
            await _context.SaveChangesAsync();

            return ApiResponse<MembershipApplicationDto>.SuccessResponse(MapToDto(application));
        }
        catch (Exception ex)
        {
            return ApiResponse<MembershipApplicationDto>.ErrorResponse(
                "Failed to submit membership application",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<MembershipApplicationDto>>> GetAllAsync(
        MembershipApplicationStatus? status = null)
    {
        try
        {
            var query = _context.MembershipApplications.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var applications = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return ApiResponse<List<MembershipApplicationDto>>.SuccessResponse(
                applications.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<MembershipApplicationDto>>.ErrorResponse(
                "Failed to retrieve membership applications",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<PagedResult<MembershipApplicationDto>>> SearchAsync(
        int page,
        int pageSize,
        string? search,
        string? sort,
        MembershipApplicationStatus? status = null)
    {
        try
        {
            (page, pageSize) = Pagination.Normalize(page, pageSize);
            var query = _context.MembershipApplications.AsNoTracking().AsQueryable();
            if (status.HasValue) query = query.Where(a => a.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a =>
                    a.FirstName.ToLower().Contains(term) || a.LastName.ToLower().Contains(term) ||
                    a.Email.ToLower().Contains(term) || (a.City != null && a.City.ToLower().Contains(term)) ||
                    (a.Province != null && a.Province.ToLower().Contains(term)));
            }

            query = sort?.ToLowerInvariant() switch
            {
                "name" => query.OrderBy(a => a.LastName).ThenBy(a => a.FirstName),
                "oldest" => query.OrderBy(a => a.CreatedAt),
                _ => query.OrderByDescending(a => a.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return ApiResponse<PagedResult<MembershipApplicationDto>>.SuccessResponse(
                PagedResult<MembershipApplicationDto>.Create(items.Select(MapToDto).ToList(), page, pageSize, total));
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<MembershipApplicationDto>>.ErrorResponse(
                "Failed to retrieve membership applications", new() { ex.Message });
        }
    }

    public async Task<ApiResponse<MembershipApplicationDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var application = await _context.MembershipApplications.FindAsync(id);
            if (application == null)
            {
                return ApiResponse<MembershipApplicationDto>.ErrorResponse("Application not found");
            }

            return ApiResponse<MembershipApplicationDto>.SuccessResponse(MapToDto(application));
        }
        catch (Exception ex)
        {
            return ApiResponse<MembershipApplicationDto>.ErrorResponse(
                "Failed to retrieve membership application",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<MemberDto>> ApproveAsync(Guid id)
    {
        try
        {
            var application = await _context.MembershipApplications.FindAsync(id);
            if (application == null)
            {
                return ApiResponse<MemberDto>.ErrorResponse("Application not found");
            }

            if (application.Status != MembershipApplicationStatus.Pending)
            {
                return ApiResponse<MemberDto>.ErrorResponse("Application has already been reviewed");
            }

            var normalizedEmail = application.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user is null && string.IsNullOrWhiteSpace(application.PasswordHash))
            {
                return ApiResponse<MemberDto>.ErrorResponse(
                    "The application predates member accounts. Ask the applicant to submit a new application with a password.");
            }

            var existingMember = await _context.Members
                .FirstOrDefaultAsync(m => m.Email.ToLower() == normalizedEmail);
            if (existingMember != null)
            {
                if (user is null)
                {
                    user = new User
                    {
                        Email = application.Email.Trim(),
                        PasswordHash = application.PasswordHash!,
                        FirstName = application.FirstName,
                        LastName = application.LastName,
                        MemberId = existingMember.Id,
                        IsAdmin = false
                    };
                    _context.Users.Add(user);
                }
                else
                {
                    user.MemberId = existingMember.Id;
                }

                application.Status = MembershipApplicationStatus.Approved;
                application.MemberId = existingMember.Id;
                application.PasswordHash = null;
                application.ReviewedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ApiResponse<MemberDto>.SuccessResponse(MapMemberToDto(existingMember));
            }

            var member = new Member
            {
                FirstName = application.FirstName,
                LastName = application.LastName,
                Email = application.Email,
                Phone = application.Phone,
                City = application.City,
                Province = application.Province,
                Profession = application.Profession,
                Expertise = application.Expertise,
                Interests = application.Motivation,
                CreatedAt = DateTime.UtcNow
            };

            _context.Members.Add(member);
            if (user is null)
            {
                user = new User
                {
                    Email = application.Email.Trim(),
                    PasswordHash = application.PasswordHash!,
                    FirstName = application.FirstName,
                    LastName = application.LastName,
                    MemberId = member.Id,
                    IsAdmin = false
                };
                _context.Users.Add(user);
            }
            else
            {
                user.MemberId = member.Id;
                user.FirstName = application.FirstName;
                user.LastName = application.LastName;
            }

            application.Status = MembershipApplicationStatus.Approved;
            application.MemberId = member.Id;
            application.PasswordHash = null;
            application.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<MemberDto>.SuccessResponse(MapMemberToDto(member));
        }
        catch (Exception ex)
        {
            return ApiResponse<MemberDto>.ErrorResponse(
                "Failed to approve membership application",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<MembershipApplicationDto>> RejectAsync(Guid id)
    {
        try
        {
            var application = await _context.MembershipApplications.FindAsync(id);
            if (application == null)
            {
                return ApiResponse<MembershipApplicationDto>.ErrorResponse("Application not found");
            }

            if (application.Status != MembershipApplicationStatus.Pending)
            {
                return ApiResponse<MembershipApplicationDto>.ErrorResponse("Application has already been reviewed");
            }

            application.Status = MembershipApplicationStatus.Rejected;
            application.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<MembershipApplicationDto>.SuccessResponse(MapToDto(application));
        }
        catch (Exception ex)
        {
            return ApiResponse<MembershipApplicationDto>.ErrorResponse(
                "Failed to reject membership application",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        try
        {
            var application = await _context.MembershipApplications.FindAsync(id);
            if (application == null)
            {
                return ApiResponse<bool>.ErrorResponse("Application not found");
            }

            _context.MembershipApplications.Remove(application);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Failed to delete membership application",
                new List<string> { ex.Message });
        }
    }

    private static MembershipApplicationDto MapToDto(MembershipApplication application)
    {
        return new MembershipApplicationDto(
            application.Id,
            application.FirstName,
            application.LastName,
            application.Email,
            application.Phone,
            application.City,
            application.Province,
            application.Profession,
            application.Expertise,
            application.Motivation,
            application.Status.ToString(),
            application.MemberId,
            application.CreatedAt,
            application.ReviewedAt
        );
    }

    private static MemberDto MapMemberToDto(Member member)
    {
        return new MemberDto(
            member.Id,
            member.FirstName,
            member.LastName,
            member.Email,
            member.Phone,
            member.City,
            member.Province,
            member.Profession,
            member.Expertise,
            member.Interests,
            member.Availability,
            member.Zone,
            member.IsAdmin,
            member.CreatedAt
        );
    }
}
