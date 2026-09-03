using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;
namespace HcbeApi.Services;

public sealed class OpportunityService(ApplicationDbContext context, INotificationService notifications) : IOpportunityService
{
    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase) { "Volunteer", "Job", "Business", "Training", "Community" };
    private static readonly HashSet<string> Statuses = new(StringComparer.OrdinalIgnoreCase) { "Draft", "Published", "Closed" };
    public async Task<ApiResponse<List<OpportunityDto>>> GetPublishedAsync(string? type)
    {
        var query = Query().Where(item => item.Status == "Published" && (item.DeadlineUtc == null || item.DeadlineUtc >= DateTime.UtcNow));
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(item => item.Type == type);
        var items = await query.OrderBy(item => item.DeadlineUtc).ThenByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<OpportunityDto>>.SuccessResponse(items.Select(Map).ToList());
    }
    public async Task<ApiResponse<List<OpportunityDto>>> GetForAdminAsync() => ApiResponse<List<OpportunityDto>>.SuccessResponse((await Query().OrderByDescending(item => item.CreatedAt).ToListAsync()).Select(Map).ToList());
    public async Task<ApiResponse<OpportunityDto>> CreateAsync(Guid userId, UpsertOpportunityRequest request)
    {
        var validationError = Validate(request); if (validationError is not null) return ApiResponse<OpportunityDto>.ErrorResponse(validationError);
        var item = new Opportunity { CreatedByUserId = userId }; Apply(item, request); context.Opportunities.Add(item); await context.SaveChangesAsync(); return ApiResponse<OpportunityDto>.SuccessResponse(Map(item));
    }
    public async Task<ApiResponse<OpportunityDto>> UpdateAsync(Guid id, UpsertOpportunityRequest request)
    {
        var validationError = Validate(request); if (validationError is not null) return ApiResponse<OpportunityDto>.ErrorResponse(validationError);
        var item = await context.Opportunities.Include(candidate => candidate.Applications).FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (item is null) return ApiResponse<OpportunityDto>.ErrorResponse("Opportunity not found"); Apply(item, request); await context.SaveChangesAsync(); return ApiResponse<OpportunityDto>.SuccessResponse(Map(item));
    }
    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        var item = await context.Opportunities.FindAsync(id); if (item is null) return ApiResponse.CreateError("Opportunity not found"); item.Status = "Closed"; item.UpdatedAt = DateTime.UtcNow; await context.SaveChangesAsync(); return ApiResponse.CreateSuccess("Opportunity closed");
    }
    public async Task<ApiResponse<OpportunityApplicationDto>> ApplyAsync(Guid userId, Guid id, CreateOpportunityApplicationRequest request)
    {
        var memberId = await MemberIdAsync(userId); if (memberId is null) return ApiResponse<OpportunityApplicationDto>.ErrorResponse("Member account not found");
        var opportunity = await context.Opportunities.FirstOrDefaultAsync(item => item.Id == id && item.Status == "Published");
        if (opportunity is null || opportunity.DeadlineUtc < DateTime.UtcNow) return ApiResponse<OpportunityApplicationDto>.ErrorResponse("Opportunity is no longer accepting applications");
        var existing = await Applications().FirstOrDefaultAsync(item => item.OpportunityId == id && item.MemberId == memberId);
        if (existing is not null) return ApiResponse<OpportunityApplicationDto>.SuccessResponse(MapApplication(existing));
        var item = new OpportunityApplication { OpportunityId = id, MemberId = memberId.Value, Message = request.Message.Trim() }; context.OpportunityApplications.Add(item); await context.SaveChangesAsync();
        item.Opportunity = opportunity; item.Member = await context.Members.FindAsync(memberId.Value);
        await notifications.CreateNotificationAsync("opportunity", "Nouvelle candidature", opportunity.Title, item.Id, "/admin/opportunities");
        return ApiResponse<OpportunityApplicationDto>.SuccessResponse(MapApplication(item));
    }
    public async Task<ApiResponse<List<OpportunityApplicationDto>>> GetMineAsync(Guid userId)
    {
        var memberId = await MemberIdAsync(userId); var items = memberId is null ? [] : await Applications().Where(item => item.MemberId == memberId).OrderByDescending(item => item.CreatedAt).ToListAsync(); return ApiResponse<List<OpportunityApplicationDto>>.SuccessResponse(items.Select(MapApplication).ToList());
    }
    public async Task<ApiResponse<List<OpportunityApplicationDto>>> GetApplicationsAsync(Guid? opportunityId)
    {
        var query = Applications(); if (opportunityId.HasValue) query = query.Where(item => item.OpportunityId == opportunityId); var items = await query.OrderBy(item => item.Status == "Submitted" ? 0 : 1).ThenByDescending(item => item.CreatedAt).ToListAsync(); return ApiResponse<List<OpportunityApplicationDto>>.SuccessResponse(items.Select(MapApplication).ToList());
    }
    public async Task<ApiResponse<OpportunityApplicationDto>> ReviewApplicationAsync(Guid id, ReviewOpportunityApplicationRequest request)
    {
        var status = request.Status.Trim(); if (status is not ("Reviewed" or "Accepted" or "Declined")) return ApiResponse<OpportunityApplicationDto>.ErrorResponse("Unsupported application status"); var item = await Applications(true).FirstOrDefaultAsync(candidate => candidate.Id == id); if (item is null) return ApiResponse<OpportunityApplicationDto>.ErrorResponse("Application not found"); item.Status = status; item.AdminNotes = Normalize(request.AdminNotes); item.UpdatedAt = DateTime.UtcNow; await context.SaveChangesAsync(); return ApiResponse<OpportunityApplicationDto>.SuccessResponse(MapApplication(item));
    }
    private IQueryable<Opportunity> Query() => context.Opportunities.AsNoTracking().Include(item => item.Applications);
    private IQueryable<OpportunityApplication> Applications(bool tracking = false) { var query = context.OpportunityApplications.Include(item => item.Opportunity).Include(item => item.Member); return tracking ? query : query.AsNoTracking(); }
    private Task<Guid?> MemberIdAsync(Guid userId) => context.Users.AsNoTracking().Where(item => item.Id == userId && item.IsActive).Select(item => item.MemberId).SingleOrDefaultAsync();
    private static string? Validate(UpsertOpportunityRequest request) { if (!Types.Contains(request.Type)) return "Unsupported opportunity type"; if (!Statuses.Contains(request.Status)) return "Unsupported opportunity status"; return null; }
    private static void Apply(Opportunity item, UpsertOpportunityRequest request) { item.Title = request.Title.Trim(); item.TitleEn = Normalize(request.TitleEn); item.Description = request.Description.Trim(); item.DescriptionEn = Normalize(request.DescriptionEn); item.Type = Types.First(value => value.Equals(request.Type, StringComparison.OrdinalIgnoreCase)); item.Organization = request.Organization.Trim(); item.Location = Normalize(request.Location); item.IsRemote = request.IsRemote; item.Skills = Normalize(request.Skills); item.ApplyUrl = Normalize(request.ApplyUrl); item.DeadlineUtc = request.DeadlineUtc?.ToUniversalTime(); item.Status = Statuses.First(value => value.Equals(request.Status, StringComparison.OrdinalIgnoreCase)); item.UpdatedAt = DateTime.UtcNow; }
    private static OpportunityDto Map(Opportunity item) => new(item.Id, item.Title, item.TitleEn, item.Description, item.DescriptionEn, item.Type, item.Organization, item.Location, item.IsRemote, item.Skills, item.ApplyUrl, item.DeadlineUtc, item.Status, item.Applications.Count, item.CreatedAt, item.UpdatedAt);
    private static OpportunityApplicationDto MapApplication(OpportunityApplication item) => new(item.Id, item.OpportunityId, item.Opportunity?.Title ?? "", item.MemberId, $"{item.Member?.FirstName} {item.Member?.LastName}".Trim(), item.Member?.Email ?? "", item.Message, item.Status, item.AdminNotes, item.CreatedAt, item.UpdatedAt);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
