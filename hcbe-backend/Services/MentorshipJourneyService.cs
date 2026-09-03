using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;
namespace HcbeApi.Services;
public sealed class MentorshipJourneyService(ApplicationDbContext context, INotificationService notifications) : IMentorshipJourneyService
{
    public async Task<ApiResponse<MentorshipJourneyDto>> GetAsync(Guid userId, Guid matchId)
    {
        var memberId = await MemberIdAsync(userId); if (memberId is null || !await CanAccessAsync(memberId.Value, matchId)) return ApiResponse<MentorshipJourneyDto>.ErrorResponse("Active mentorship match not found");
        var goals = await context.MentorshipGoals.AsNoTracking().Where(item => item.MatchId == matchId).OrderBy(item => item.CreatedAt).ToListAsync();
        var checkIns = await context.MentorshipCheckIns.AsNoTracking().Include(item => item.Member).Where(item => item.MatchId == matchId).OrderByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<MentorshipJourneyDto>.SuccessResponse(new(matchId, goals.Select(MapGoal).ToList(), checkIns.Select(MapCheckIn).ToList()));
    }
    public async Task<ApiResponse<MentorshipGoalDto>> AddGoalAsync(Guid userId, Guid matchId, CreateMentorshipGoalRequest request)
    {
        var memberId = await MemberIdAsync(userId); if (memberId is null || !await CanAccessAsync(memberId.Value, matchId)) return ApiResponse<MentorshipGoalDto>.ErrorResponse("Active mentorship match not found");
        var item = new MentorshipGoal { MatchId = matchId, CreatedByMemberId = memberId.Value, Title = request.Title.Trim(), DueAtUtc = request.DueAtUtc?.ToUniversalTime() }; context.MentorshipGoals.Add(item); await context.SaveChangesAsync(); return ApiResponse<MentorshipGoalDto>.SuccessResponse(MapGoal(item));
    }
    public async Task<ApiResponse<MentorshipGoalDto>> UpdateGoalAsync(Guid userId, Guid goalId, UpdateMentorshipGoalRequest request)
    {
        var memberId = await MemberIdAsync(userId); var item = await context.MentorshipGoals.FindAsync(goalId); if (memberId is null || item is null || !await CanAccessAsync(memberId.Value, item.MatchId)) return ApiResponse<MentorshipGoalDto>.ErrorResponse("Mentorship goal not found"); var status = request.Status.Trim(); if (status is not ("Open" or "Completed" or "Cancelled")) return ApiResponse<MentorshipGoalDto>.ErrorResponse("Unsupported goal status"); item.Status = status; item.UpdatedAt = DateTime.UtcNow; await context.SaveChangesAsync(); return ApiResponse<MentorshipGoalDto>.SuccessResponse(MapGoal(item));
    }
    public async Task<ApiResponse<MentorshipCheckInDto>> AddCheckInAsync(Guid userId, Guid matchId, CreateMentorshipCheckInRequest request)
    {
        var memberId = await MemberIdAsync(userId); if (memberId is null || !await CanAccessAsync(memberId.Value, matchId)) return ApiResponse<MentorshipCheckInDto>.ErrorResponse("Active mentorship match not found"); var item = new MentorshipCheckIn { MatchId = matchId, MemberId = memberId.Value, Summary = request.Summary.Trim(), Rating = request.Rating, NeedsCommitteeSupport = request.NeedsCommitteeSupport }; context.MentorshipCheckIns.Add(item); await context.SaveChangesAsync(); item.Member = await context.Members.FindAsync(memberId.Value); if (item.NeedsCommitteeSupport) await notifications.CreateNotificationAsync("mentorship-support", "Suivi de mentorat requis", item.Summary, item.Id, "/admin/mentorship"); return ApiResponse<MentorshipCheckInDto>.SuccessResponse(MapCheckIn(item));
    }
    public async Task<ApiResponse<List<MentorshipCheckInDto>>> GetSupportFlagsAsync() => ApiResponse<List<MentorshipCheckInDto>>.SuccessResponse((await context.MentorshipCheckIns.AsNoTracking().Include(item => item.Member).Where(item => item.NeedsCommitteeSupport).OrderByDescending(item => item.CreatedAt).ToListAsync()).Select(MapCheckIn).ToList());
    private Task<Guid?> MemberIdAsync(Guid userId) => context.Users.AsNoTracking().Where(item => item.Id == userId && item.IsActive).Select(item => item.MemberId).SingleOrDefaultAsync();
    private Task<bool> CanAccessAsync(Guid memberId, Guid matchId) => context.MentorshipMatches.AsNoTracking().AnyAsync(item => item.Id == matchId && item.Status == "Active" && (item.MentorApplication!.MemberId == memberId || item.MenteeApplication!.MemberId == memberId));
    private static MentorshipGoalDto MapGoal(MentorshipGoal item) => new(item.Id, item.MatchId, item.CreatedByMemberId, item.Title, item.Status, item.DueAtUtc, item.CreatedAt, item.UpdatedAt);
    private static MentorshipCheckInDto MapCheckIn(MentorshipCheckIn item) => new(item.Id, item.MatchId, item.MemberId, $"{item.Member?.FirstName} {item.Member?.LastName}".Trim(), item.Summary, item.Rating, item.NeedsCommitteeSupport, item.CreatedAt);
}
