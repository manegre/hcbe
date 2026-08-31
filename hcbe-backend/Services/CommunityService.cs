using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public class CommunityService : ICommunityService
{
    private static readonly HashSet<string> Roles = new(StringComparer.OrdinalIgnoreCase) { "Mentor", "Mentee" };
    private static readonly HashSet<string> ReviewStatuses = new(StringComparer.OrdinalIgnoreCase) { "Approved", "Rejected" };
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notifications;

    public CommunityService(ApplicationDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task<ApiResponse<List<MentorshipApplicationDto>>> GetMyApplicationsAsync(Guid userId)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<MentorshipApplicationDto>>.ErrorResponse("Member profile required");
        var items = await _context.MentorshipApplications.AsNoTracking().Include(item => item.Member)
            .Where(item => item.MemberId == memberId).OrderByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<MentorshipApplicationDto>>.SuccessResponse(items.Select(MapApplication).ToList());
    }

    public async Task<ApiResponse<MentorshipApplicationDto>> ApplyForMentorshipAsync(Guid userId, CreateMentorshipApplicationRequest request)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<MentorshipApplicationDto>.ErrorResponse("Member profile required");
        var role = NormalizeRole(request.Role);
        if (role is null) return ApiResponse<MentorshipApplicationDto>.ErrorResponse("Role must be Mentor or Mentee");
        if (!request.ConsentToShare) return ApiResponse<MentorshipApplicationDto>.ErrorResponse("Consent is required for committee review");
        var exists = await _context.MentorshipApplications.AnyAsync(item => item.MemberId == memberId && item.Role == role && new[] { "Pending", "Approved", "Matched" }.Contains(item.Status));
        if (exists) return ApiResponse<MentorshipApplicationDto>.ErrorResponse("An active application already exists for this role");
        var item = new MentorshipApplication
        {
            MemberId = memberId.Value, Role = role,
            ProfessionalSummary = request.ProfessionalSummary.Trim(), Expertise = request.Expertise.Trim(),
            Objectives = request.Objectives.Trim(), Availability = request.Availability.Trim(),
            PreferredLanguage = request.PreferredLanguage.Trim().ToLowerInvariant() == "en" ? "en" : "fr",
            ConsentToShare = true
        };
        _context.MentorshipApplications.Add(item);
        await _context.SaveChangesAsync();
        item.Member = await _context.Members.FindAsync(memberId.Value);
        await _notifications.CreateNotificationAsync("mentorship", "Nouvelle candidature de mentorat", $"{item.Member?.FirstName} {item.Member?.LastName} — {role}", item.Id, "/admin/mentorship");
        return ApiResponse<MentorshipApplicationDto>.SuccessResponse(MapApplication(item));
    }

    public async Task<ApiResponse<MentorshipApplicationDto>> WithdrawApplicationAsync(Guid userId, Guid id)
    {
        var memberId = await GetMemberIdAsync(userId);
        var item = memberId is null ? null : await _context.MentorshipApplications.Include(x => x.Member).FirstOrDefaultAsync(x => x.Id == id && x.MemberId == memberId);
        if (item is null) return ApiResponse<MentorshipApplicationDto>.ErrorResponse("Application not found");
        if (item.Status is "Matched" or "Withdrawn") return ApiResponse<MentorshipApplicationDto>.ErrorResponse("This application can no longer be withdrawn");
        item.Status = "Withdrawn"; item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse<MentorshipApplicationDto>.SuccessResponse(MapApplication(item));
    }

    public async Task<ApiResponse<List<MentorshipApplicationDto>>> GetApplicationsForAdminAsync(string? role, string? status, string? search)
    {
        var query = _context.MentorshipApplications.AsNoTracking().Include(item => item.Member).AsQueryable();
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(item => item.Role == role);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item => item.Member != null && (item.Member.FirstName.ToLower().Contains(term) || item.Member.LastName.ToLower().Contains(term) || item.Member.Email.ToLower().Contains(term) || item.Expertise.ToLower().Contains(term)));
        }
        var items = await query.OrderByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<MentorshipApplicationDto>>.SuccessResponse(items.Select(MapApplication).ToList());
    }

    public async Task<ApiResponse<MentorshipApplicationDto>> ReviewApplicationAsync(Guid id, ReviewMentorshipApplicationRequest request)
    {
        if (!ReviewStatuses.Contains(request.Status)) return ApiResponse<MentorshipApplicationDto>.ErrorResponse("Status must be Approved or Rejected");
        var item = await _context.MentorshipApplications.Include(x => x.Member).FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return ApiResponse<MentorshipApplicationDto>.ErrorResponse("Application not found");
        if (item.Status == "Matched") return ApiResponse<MentorshipApplicationDto>.ErrorResponse("A matched application cannot be reviewed again");
        item.Status = Canonical(request.Status); item.CommitteeNotes = Normalize(request.CommitteeNotes);
        item.ReviewedAt = DateTime.UtcNow; item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse<MentorshipApplicationDto>.SuccessResponse(MapApplication(item));
    }

    public async Task<ApiResponse<List<MentorshipMatchDto>>> GetMyMatchesAsync(Guid userId)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<MentorshipMatchDto>>.ErrorResponse("Member profile required");
        var items = await MatchesQuery().Where(item => item.MentorApplication!.MemberId == memberId || item.MenteeApplication!.MemberId == memberId).OrderByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<MentorshipMatchDto>>.SuccessResponse(items.Select(item => MapMatch(item, memberId)).ToList());
    }

    public async Task<ApiResponse<List<MentorshipMatchDto>>> GetMatchesForAdminAsync()
    {
        var items = await MatchesQuery().OrderByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<MentorshipMatchDto>>.SuccessResponse(items.Select(item => MapMatch(item, null)).ToList());
    }

    public async Task<ApiResponse<MentorshipMatchDto>> CreateMatchAsync(CreateMentorshipMatchRequest request)
    {
        var mentor = await _context.MentorshipApplications.Include(x => x.Member).FirstOrDefaultAsync(x => x.Id == request.MentorApplicationId);
        var mentee = await _context.MentorshipApplications.Include(x => x.Member).FirstOrDefaultAsync(x => x.Id == request.MenteeApplicationId);
        if (mentor is null || mentee is null || mentor.Role != "Mentor" || mentee.Role != "Mentee") return ApiResponse<MentorshipMatchDto>.ErrorResponse("Select one valid mentor and one valid mentee");
        if (mentor.MemberId == mentee.MemberId) return ApiResponse<MentorshipMatchDto>.ErrorResponse("A member cannot be matched with themselves");
        if (mentor.Status != "Approved" || mentee.Status != "Approved") return ApiResponse<MentorshipMatchDto>.ErrorResponse("Both applications must be approved");
        var exists = await _context.MentorshipMatches.AnyAsync(x =>
            (x.Status == "Proposed" || x.Status == "Active") &&
            (x.MentorApplicationId == mentor.Id || x.MenteeApplicationId == mentee.Id));
        if (exists) return ApiResponse<MentorshipMatchDto>.ErrorResponse("One of these applications already has an active proposal");
        var match = new MentorshipMatch { MentorApplicationId = mentor.Id, MenteeApplicationId = mentee.Id, MentorApplication = mentor, MenteeApplication = mentee, CommitteeNotes = Normalize(request.CommitteeNotes) };
        _context.MentorshipMatches.Add(match);
        await _context.SaveChangesAsync();
        return ApiResponse<MentorshipMatchDto>.SuccessResponse(MapMatch(match, null));
    }

    public async Task<ApiResponse<MentorshipMatchDto>> RespondToMatchAsync(Guid userId, Guid id, string response)
    {
        var memberId = await GetMemberIdAsync(userId);
        var match = await MatchesQuery(false).FirstOrDefaultAsync(item => item.Id == id);
        if (memberId is null || match is null) return ApiResponse<MentorshipMatchDto>.ErrorResponse("Match not found");
        var isMentor = match.MentorApplication!.MemberId == memberId;
        var isMentee = match.MenteeApplication!.MemberId == memberId;
        if (!isMentor && !isMentee) return ApiResponse<MentorshipMatchDto>.ErrorResponse("Match not found");
        if (match.Status != "Proposed") return ApiResponse<MentorshipMatchDto>.ErrorResponse("This proposal can no longer be changed");
        if (response.Equals("Decline", StringComparison.OrdinalIgnoreCase))
        {
            match.Status = "Declined";
        }
        else if (response.Equals("Accept", StringComparison.OrdinalIgnoreCase))
        {
            if (isMentor) match.MentorAccepted = true;
            if (isMentee) match.MenteeAccepted = true;
            if (match.MentorAccepted && match.MenteeAccepted)
            {
                match.Status = "Active"; match.ActivatedAt = DateTime.UtcNow;
                match.MentorApplication.Status = "Matched"; match.MenteeApplication.Status = "Matched";
            }
        }
        else return ApiResponse<MentorshipMatchDto>.ErrorResponse("Response must be Accept or Decline");
        match.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse<MentorshipMatchDto>.SuccessResponse(MapMatch(match, memberId));
    }

    public async Task<ApiResponse<MentorshipMatchDto>> UpdateMatchStatusAsync(Guid id, UpdateMentorshipMatchStatusRequest request)
    {
        var allowed = new[] { "Completed", "Cancelled" };
        var status = allowed.FirstOrDefault(item => item.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        if (status is null) return ApiResponse<MentorshipMatchDto>.ErrorResponse("Status must be Completed or Cancelled");
        var match = await MatchesQuery(false).FirstOrDefaultAsync(item => item.Id == id);
        if (match is null) return ApiResponse<MentorshipMatchDto>.ErrorResponse("Match not found");
        match.Status = status; match.UpdatedAt = DateTime.UtcNow;
        if (status == "Completed") match.CompletedAt = DateTime.UtcNow;
        if (status == "Cancelled")
        {
            if (match.MentorApplication!.Status == "Matched") match.MentorApplication.Status = "Approved";
            if (match.MenteeApplication!.Status == "Matched") match.MenteeApplication.Status = "Approved";
        }
        await _context.SaveChangesAsync();
        return ApiResponse<MentorshipMatchDto>.SuccessResponse(MapMatch(match, null));
    }

    public async Task<ApiResponse<NetworkingProfileDto>> GetMyNetworkingProfileAsync(Guid userId)
    {
        var memberId = await GetMemberIdAsync(userId);
        var profile = memberId is null ? null : await _context.NetworkingProfiles.AsNoTracking().Include(x => x.Member).FirstOrDefaultAsync(x => x.MemberId == memberId);
        return profile is null ? ApiResponse<NetworkingProfileDto>.ErrorResponse("Networking profile not found") : ApiResponse<NetworkingProfileDto>.SuccessResponse(MapProfile(profile));
    }

    public async Task<ApiResponse<NetworkingProfileDto>> UpsertNetworkingProfileAsync(Guid userId, UpsertNetworkingProfileRequest request)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<NetworkingProfileDto>.ErrorResponse("Member profile required");
        var profile = await _context.NetworkingProfiles.Include(x => x.Member).FirstOrDefaultAsync(x => x.MemberId == memberId);
        if (profile is null)
        {
            profile = new NetworkingProfile { MemberId = memberId.Value, Member = await _context.Members.FindAsync(memberId.Value) };
            _context.NetworkingProfiles.Add(profile);
        }
        profile.Headline = request.Headline.Trim(); profile.Bio = request.Bio.Trim();
        profile.Expertise = request.Expertise.Trim(); profile.Sectors = request.Sectors.Trim();
        profile.City = Normalize(request.City); profile.Province = Normalize(request.Province);
        profile.IsVisible = request.IsVisible; profile.AllowContactRequests = request.AllowContactRequests;
        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse<NetworkingProfileDto>.SuccessResponse(MapProfile(profile));
    }

    public async Task<ApiResponse<List<NetworkingProfileDto>>> SearchDirectoryAsync(Guid userId, string? search, string? province)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<NetworkingProfileDto>>.ErrorResponse("Member profile required");
        var query = _context.NetworkingProfiles.AsNoTracking().Include(x => x.Member).Where(x => x.IsVisible && x.AllowContactRequests && x.MemberId != memberId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Headline.ToLower().Contains(term) || x.Expertise.ToLower().Contains(term) || x.Sectors.ToLower().Contains(term) || x.Member!.FirstName.ToLower().Contains(term) || x.Member.LastName.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(province)) query = query.Where(x => x.Province == province);
        var profiles = await query.OrderBy(x => x.Member!.LastName).Take(100).ToListAsync();
        return ApiResponse<List<NetworkingProfileDto>>.SuccessResponse(profiles.Select(MapProfile).ToList());
    }

    public async Task<ApiResponse<ConnectionRequestDto>> CreateConnectionRequestAsync(Guid userId, CreateConnectionRequestRequest request)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<ConnectionRequestDto>.ErrorResponse("Member profile required");
        if (memberId == request.RecipientMemberId) return ApiResponse<ConnectionRequestDto>.ErrorResponse("You cannot contact yourself");
        var recipient = await _context.NetworkingProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.MemberId == request.RecipientMemberId && x.IsVisible && x.AllowContactRequests);
        if (recipient is null) return ApiResponse<ConnectionRequestDto>.ErrorResponse("This member is not accepting contact requests");
        var duplicate = await _context.ConnectionRequests.AnyAsync(x => x.RequesterMemberId == memberId && x.RecipientMemberId == request.RecipientMemberId && x.Status == "Pending");
        if (duplicate) return ApiResponse<ConnectionRequestDto>.ErrorResponse("A contact request is already pending");
        var item = new ConnectionRequest { RequesterMemberId = memberId.Value, RecipientMemberId = request.RecipientMemberId, Message = request.Message.Trim() };
        _context.ConnectionRequests.Add(item);
        await _context.SaveChangesAsync();
        item = await RequestsQuery().FirstAsync(x => x.Id == item.Id);
        return ApiResponse<ConnectionRequestDto>.SuccessResponse(MapRequest(item, memberId.GetValueOrDefault()));
    }

    public async Task<ApiResponse<List<ConnectionRequestDto>>> GetMyConnectionRequestsAsync(Guid userId)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<ConnectionRequestDto>>.ErrorResponse("Member profile required");
        var items = await RequestsQuery().Where(x => x.RequesterMemberId == memberId || x.RecipientMemberId == memberId).OrderByDescending(x => x.CreatedAt).ToListAsync();
        return ApiResponse<List<ConnectionRequestDto>>.SuccessResponse(items.Select(x => MapRequest(x, memberId.Value)).ToList());
    }

    public async Task<ApiResponse<ConnectionRequestDto>> RespondToConnectionRequestAsync(Guid userId, Guid id, RespondConnectionRequestRequest request)
    {
        var memberId = await GetMemberIdAsync(userId);
        var item = memberId is null ? null : await RequestsQuery().FirstOrDefaultAsync(x => x.Id == id && x.RecipientMemberId == memberId);
        if (item is null) return ApiResponse<ConnectionRequestDto>.ErrorResponse("Contact request not found");
        if (item.Status != "Pending") return ApiResponse<ConnectionRequestDto>.ErrorResponse("This request has already been answered");
        var status = request.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase) ? "Accepted" : request.Status.Equals("Declined", StringComparison.OrdinalIgnoreCase) ? "Declined" : null;
        if (status is null) return ApiResponse<ConnectionRequestDto>.ErrorResponse("Status must be Accepted or Declined");
        item.Status = status; item.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse<ConnectionRequestDto>.SuccessResponse(MapRequest(item, memberId.GetValueOrDefault()));
    }

    private Task<Guid?> GetMemberIdAsync(Guid userId) => _context.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.MemberId).FirstOrDefaultAsync();
    private IQueryable<MentorshipMatch> MatchesQuery(bool noTracking = true)
    {
        var query = _context.MentorshipMatches
            .Include(x => x.MentorApplication).ThenInclude(x => x!.Member)
            .Include(x => x.MenteeApplication).ThenInclude(x => x!.Member)
            .AsQueryable();
        return noTracking ? query.AsNoTracking() : query;
    }
    private IQueryable<ConnectionRequest> RequestsQuery() => _context.ConnectionRequests.Include(x => x.RequesterMember).Include(x => x.RecipientMember);
    private static MentorshipApplicationDto MapApplication(MentorshipApplication item) => new(item.Id, item.MemberId, Name(item.Member), item.Member?.Email, item.Role, item.ProfessionalSummary, item.Expertise, item.Objectives, item.Availability, item.PreferredLanguage, item.ConsentToShare, item.Status, item.CommitteeNotes, item.CreatedAt, item.UpdatedAt, item.ReviewedAt);
    private static MentorshipMatchDto MapMatch(MentorshipMatch item, Guid? viewerMemberId)
    {
        var mentor = item.MentorApplication!.Member; var mentee = item.MenteeApplication!.Member;
        var counterpart = viewerMemberId == item.MentorApplication.MemberId ? mentee : viewerMemberId == item.MenteeApplication.MemberId ? mentor : null;
        return new(item.Id, item.MentorApplicationId, item.MenteeApplicationId, Name(mentor), Name(mentee), item.Status, item.MentorAccepted, item.MenteeAccepted, item.CommitteeNotes, counterpart is null ? null : Name(counterpart), item.Status == "Active" ? counterpart?.Email : null, item.CreatedAt, item.UpdatedAt, item.ActivatedAt, item.CompletedAt);
    }
    private static NetworkingProfileDto MapProfile(NetworkingProfile item) => new(item.Id, item.MemberId, Name(item.Member), item.Headline, item.Bio, item.Expertise, item.Sectors, item.City, item.Province, item.IsVisible, item.AllowContactRequests, item.UpdatedAt);
    private static ConnectionRequestDto MapRequest(ConnectionRequest item, Guid viewerMemberId)
    {
        var direction = item.RequesterMemberId == viewerMemberId ? "Sent" : "Received";
        var counterpart = direction == "Sent" ? item.RecipientMember : item.RequesterMember;
        return new(item.Id, item.RequesterMemberId, item.RecipientMemberId, Name(item.RequesterMember), Name(item.RecipientMember), item.Message, item.Status, direction, item.Status == "Accepted" ? counterpart?.Email : null, item.CreatedAt, item.RespondedAt);
    }
    private static string Name(Member? member) => member is null ? "" : $"{member.FirstName} {member.LastName}".Trim();
    private static string? NormalizeRole(string role) => Roles.FirstOrDefault(item => item.Equals(role, StringComparison.OrdinalIgnoreCase));
    private static string Canonical(string value) => char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
