using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class TeamMemberService : ITeamMemberService
{
    private readonly ApplicationDbContext _context;

    public TeamMemberService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<TeamMemberDto>>> GetAllAsync()
    {
        try
        {
            var members = await _context.TeamMembers
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Name)
                .ToListAsync();

            var memberDtos = members.Select(MapToDto).ToList();
            return ApiResponse<List<TeamMemberDto>>.SuccessResponse(memberDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<TeamMemberDto>>.ErrorResponse(
                "Failed to retrieve team members",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<TeamMemberDto>>> GetActiveAsync()
    {
        try
        {
            var members = await _context.TeamMembers
                .Where(m => m.IsActive)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Name)
                .ToListAsync();

            var memberDtos = members.Select(MapToDto).ToList();
            return ApiResponse<List<TeamMemberDto>>.SuccessResponse(memberDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<TeamMemberDto>>.ErrorResponse(
                "Failed to retrieve active team members",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<TeamMemberDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var member = await _context.TeamMembers.FindAsync(id);
            if (member == null)
            {
                return ApiResponse<TeamMemberDto>.ErrorResponse("Team member not found");
            }

            return ApiResponse<TeamMemberDto>.SuccessResponse(MapToDto(member));
        }
        catch (Exception ex)
        {
            return ApiResponse<TeamMemberDto>.ErrorResponse(
                "Failed to retrieve team member",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<TeamMemberDto>> CreateAsync(CreateTeamMemberRequest request)
    {
        try
        {
            var member = new TeamMember
            {
                Name = request.Name,
                Position = request.Position,
                PositionEn = Normalize(request.PositionEn),
                Region = request.Region,
                RegionEn = Normalize(request.RegionEn),
                Zone = request.Zone,
                ZoneEn = Normalize(request.ZoneEn),
                Photo = request.Photo,
                Bio = request.Bio,
                BioEn = Normalize(request.BioEn),
                Email = request.Email,
                Order = request.Order,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TeamMembers.Add(member);
            await _context.SaveChangesAsync();

            return ApiResponse<TeamMemberDto>.SuccessResponse(MapToDto(member));
        }
        catch (Exception ex)
        {
            return ApiResponse<TeamMemberDto>.ErrorResponse(
                "Failed to create team member",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<TeamMemberDto>> UpdateAsync(Guid id, UpdateTeamMemberRequest request)
    {
        try
        {
            var member = await _context.TeamMembers.FindAsync(id);
            if (member == null)
            {
                return ApiResponse<TeamMemberDto>.ErrorResponse("Team member not found");
            }

            if (request.Name != null) member.Name = request.Name;
            if (request.Position != null) member.Position = request.Position;
            if (request.PositionEn != null) member.PositionEn = Normalize(request.PositionEn);
            if (request.Region != null) member.Region = request.Region;
            if (request.RegionEn != null) member.RegionEn = Normalize(request.RegionEn);
            if (request.Zone != null) member.Zone = request.Zone;
            if (request.ZoneEn != null) member.ZoneEn = Normalize(request.ZoneEn);
            if (request.Photo != null) member.Photo = request.Photo;
            if (request.Bio != null) member.Bio = request.Bio;
            if (request.BioEn != null) member.BioEn = Normalize(request.BioEn);
            if (request.Email != null) member.Email = request.Email;
            if (request.IsActive.HasValue) member.IsActive = request.IsActive.Value;
            if (request.Order.HasValue) member.Order = request.Order.Value;

            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<TeamMemberDto>.SuccessResponse(MapToDto(member));
        }
        catch (Exception ex)
        {
            return ApiResponse<TeamMemberDto>.ErrorResponse(
                "Failed to update team member",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        try
        {
            var member = await _context.TeamMembers.FindAsync(id);
            if (member == null)
            {
                return ApiResponse<bool>.ErrorResponse("Team member not found");
            }

            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Failed to delete team member",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<bool>> ToggleStatusAsync(Guid id)
    {
        try
        {
            var member = await _context.TeamMembers.FindAsync(id);
            if (member == null)
            {
                return ApiResponse<bool>.ErrorResponse("Team member not found");
            }

            member.IsActive = !member.IsActive;
            member.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Failed to toggle team member status",
                new List<string> { ex.Message });
        }
    }

    private static TeamMemberDto MapToDto(TeamMember member)
    {
        return new TeamMemberDto(
            member.Id,
            member.Name,
            member.Position,
            member.Region,
            member.Zone,
            member.Photo,
            member.Bio,
            member.Email,
            member.IsActive,
            member.Order,
            member.CreatedAt,
            member.UpdatedAt,
            member.PositionEn,
            member.RegionEn,
            member.ZoneEn,
            member.BioEn
        );
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
