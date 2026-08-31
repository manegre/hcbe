using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class MemberService : IMemberService
{
    private readonly ApplicationDbContext _context;

    public MemberService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<MemberDto>>> GetAllAsync()
    {
        try
        {
            var members = await _context.Members
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var memberDtos = members.Select(MapToDto).ToList();
            return ApiResponse<List<MemberDto>>.SuccessResponse(memberDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<MemberDto>>.ErrorResponse(
                "Failed to retrieve members", 
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<PagedResult<MemberDto>>> SearchAsync(
        int page, int pageSize, string? search, string? sort)
    {
        try
        {
            (page, pageSize) = Pagination.Normalize(page, pageSize);
            var query = _context.Members.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(m =>
                    m.FirstName.ToLower().Contains(term) || m.LastName.ToLower().Contains(term) ||
                    m.Email.ToLower().Contains(term) || (m.City != null && m.City.ToLower().Contains(term)) ||
                    (m.Province != null && m.Province.ToLower().Contains(term)));
            }

            query = sort?.ToLowerInvariant() switch
            {
                "name" => query.OrderBy(m => m.LastName).ThenBy(m => m.FirstName),
                "oldest" => query.OrderBy(m => m.CreatedAt),
                _ => query.OrderByDescending(m => m.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return ApiResponse<PagedResult<MemberDto>>.SuccessResponse(
                PagedResult<MemberDto>.Create(items.Select(MapToDto).ToList(), page, pageSize, total));
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<MemberDto>>.ErrorResponse("Failed to retrieve members", new() { ex.Message });
        }
    }

    public async Task<ApiResponse<MemberDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return ApiResponse<MemberDto>.ErrorResponse("Member not found");
            }

            return ApiResponse<MemberDto>.SuccessResponse(MapToDto(member));
        }
        catch (Exception ex)
        {
            return ApiResponse<MemberDto>.ErrorResponse(
                "Failed to retrieve member",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<MemberDto>> CreateAsync(CreateMemberRequest request)
    {
        try
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var emailExists = await _context.Members
                .AnyAsync(m => m.Email.ToLower() == normalizedEmail);
            if (emailExists)
            {
                return ApiResponse<MemberDto>.ErrorResponse("A member with this email already exists");
            }

            var member = new Member
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone?.Trim(),
                City = request.City?.Trim(),
                Province = request.Province?.Trim(),
                Profession = request.Profession?.Trim(),
                Expertise = request.Expertise?.Trim(),
                Interests = request.Interests?.Trim(),
                Availability = request.Availability?.Trim(),
                Zone = request.Zone?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            return ApiResponse<MemberDto>.SuccessResponse(MapToDto(member));
        }
        catch (Exception ex)
        {
            return ApiResponse<MemberDto>.ErrorResponse(
                "Failed to create member",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<MemberDto>> UpdateAsync(Guid id, UpdateMemberRequest request)
    {
        try
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return ApiResponse<MemberDto>.ErrorResponse("Member not found");
            }

            if (request.Email != null)
            {
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                var emailExists = await _context.Members
                    .AnyAsync(m => m.Id != id && m.Email.ToLower() == normalizedEmail);
                if (emailExists)
                {
                    return ApiResponse<MemberDto>.ErrorResponse("A member with this email already exists");
                }
                member.Email = request.Email.Trim();
            }

            if (request.FirstName != null) member.FirstName = request.FirstName.Trim();
            if (request.LastName != null) member.LastName = request.LastName.Trim();
            if (request.Phone != null) member.Phone = request.Phone.Trim();
            if (request.City != null) member.City = request.City.Trim();
            if (request.Province != null) member.Province = request.Province.Trim();
            if (request.Profession != null) member.Profession = request.Profession.Trim();
            if (request.Expertise != null) member.Expertise = request.Expertise.Trim();
            if (request.Interests != null) member.Interests = request.Interests.Trim();
            if (request.Availability != null) member.Availability = request.Availability.Trim();
            if (request.Zone != null) member.Zone = request.Zone.Trim();
            if (request.IsAdmin.HasValue) member.IsAdmin = request.IsAdmin.Value;

            await _context.SaveChangesAsync();

            return ApiResponse<MemberDto>.SuccessResponse(MapToDto(member));
        }
        catch (Exception ex)
        {
            return ApiResponse<MemberDto>.ErrorResponse(
                "Failed to update member",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        try
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return ApiResponse<bool>.ErrorResponse("Member not found");
            }

            _context.Members.Remove(member);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Failed to delete member",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<MemberDto>> UpdateAdminStatusAsync(Guid id, bool isAdmin)
    {
        try
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return ApiResponse<MemberDto>.ErrorResponse("Member not found");
            }

            member.IsAdmin = isAdmin;
            await _context.SaveChangesAsync();

            return ApiResponse<MemberDto>.SuccessResponse(MapToDto(member));
        }
        catch (Exception ex)
        {
            return ApiResponse<MemberDto>.ErrorResponse(
                "Failed to update member admin status",
                new List<string> { ex.Message });
        }
    }

    private static MemberDto MapToDto(Member member)
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

