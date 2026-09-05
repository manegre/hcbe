using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class ConsultationService(ApplicationDbContext context) : IConsultationService
{
    private static readonly HashSet<string> GovernanceTypes = new(StringComparer.OrdinalIgnoreCase) { "Information", "Survey", "Proposal", "Vote" };
    private static readonly HashSet<string> VotingModes = new(StringComparer.OrdinalIgnoreCase) { "Named", "Anonymous" };
    private static readonly HashSet<string> EligibilityRules = new(StringComparer.OrdinalIgnoreCase) { "AllMembers", "ActiveMembers", "Administrators" };

    public async Task<ApiResponse<List<ConsultationDto>>> GetActiveAsync(Guid? userId = null)
    {
        try
        {
            var items = await Query().AsNoTracking().Where(item => item.IsActive)
                .OrderBy(item => item.DisplayOrder).ThenBy(item => item.Title).ToListAsync();
            var mapped = new List<ConsultationDto>();
            foreach (var item in items) mapped.Add(await MapAsync(item, userId, false));
            return ApiResponse<List<ConsultationDto>>.SuccessResponse(mapped);
        }
        catch (Exception exception)
        {
            return ApiResponse<List<ConsultationDto>>.ErrorResponse("Failed to retrieve consultations", [exception.Message]);
        }
    }

    public async Task<ApiResponse<List<ConsultationDto>>> GetAllForAdminAsync()
    {
        try
        {
            var items = await Query().AsNoTracking().OrderBy(item => item.DisplayOrder).ThenBy(item => item.Title).ToListAsync();
            var mapped = new List<ConsultationDto>();
            foreach (var item in items) mapped.Add(await MapAsync(item, null, true));
            return ApiResponse<List<ConsultationDto>>.SuccessResponse(mapped);
        }
        catch (Exception exception)
        {
            return ApiResponse<List<ConsultationDto>>.ErrorResponse("Failed to retrieve consultations", [exception.Message]);
        }
    }

    public async Task<ApiResponse<ConsultationDto>> GetByIdAsync(Guid id, Guid? userId = null)
    {
        var item = await Query().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == id && candidate.IsActive);
        return item is null
            ? ApiResponse<ConsultationDto>.ErrorResponse("Consultation not found")
            : ApiResponse<ConsultationDto>.SuccessResponse(await MapAsync(item, userId, false));
    }

    public async Task<ApiResponse<ConsultationDto>> GetByIdForAdminAsync(Guid id)
    {
        var item = await Query().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == id);
        return item is null
            ? ApiResponse<ConsultationDto>.ErrorResponse("Consultation not found")
            : ApiResponse<ConsultationDto>.SuccessResponse(await MapAsync(item, null, true));
    }

    public async Task<ApiResponse<ConsultationDto>> CreateAsync(CreateConsultationRequest request, Guid userId)
    {
        var validation = Validate(request.GovernanceType, request.OpensAtUtc, request.ClosesAtUtc,
            request.CommentClosesAtUtc, request.QuorumPercentage, request.MinimumParticipation, request.Options);
        if (validation is not null) return ApiResponse<ConsultationDto>.ErrorResponse(validation);

        var item = new Consultation
        {
            Title = request.Title.Trim(), TitleEn = Optional(request.TitleEn),
            Description = request.Description.Trim(), DescriptionEn = Optional(request.DescriptionEn),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? "ri-chat-poll-line" : request.Icon.Trim(),
            LayoutType = Layout(request.LayoutType), ActionUrl = Optional(request.ActionUrl),
            ActionLabel = Optional(request.ActionLabel), ActionLabelEn = Optional(request.ActionLabelEn),
            SecondaryActionUrl = Optional(request.SecondaryActionUrl),
            SecondaryActionLabel = Optional(request.SecondaryActionLabel), SecondaryActionLabelEn = Optional(request.SecondaryActionLabelEn),
            AccentColor = Accent(request.AccentColor), DisplayOrder = request.DisplayOrder, IsActive = request.IsActive,
            GovernanceType = Governance(request.GovernanceType), OpensAtUtc = request.OpensAtUtc?.ToUniversalTime(),
            ClosesAtUtc = request.ClosesAtUtc?.ToUniversalTime(), CommentClosesAtUtc = request.CommentClosesAtUtc?.ToUniversalTime(),
            VotingMode = Voting(request.VotingMode), EligibilityRule = Eligibility(request.EligibilityRule),
            QuorumPercentage = Math.Clamp(request.QuorumPercentage, 0, 100),
            MinimumParticipation = Math.Max(0, request.MinimumParticipation), AllowComments = request.AllowComments,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        SetOptions(item, request.Options);
        item.AuditEvents.Add(new ConsultationAuditEvent { UserId = userId, Action = "Created", Details = item.GovernanceType });
        context.Consultations.Add(item);
        await context.SaveChangesAsync();
        return ApiResponse<ConsultationDto>.SuccessResponse(await MapAsync(item, null, true));
    }

    public async Task<ApiResponse<ConsultationDto>> UpdateAsync(Guid id, UpdateConsultationRequest request, Guid userId)
    {
        var item = await Query().FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (item is null) return ApiResponse<ConsultationDto>.ErrorResponse("Consultation not found");
        var governanceType = request.GovernanceType ?? item.GovernanceType;
        var validation = Validate(governanceType, request.OpensAtUtc ?? item.OpensAtUtc, request.ClosesAtUtc ?? item.ClosesAtUtc,
            request.CommentClosesAtUtc ?? item.CommentClosesAtUtc, request.QuorumPercentage ?? item.QuorumPercentage,
            request.MinimumParticipation ?? item.MinimumParticipation,
            request.Options ?? item.Options.Select(option => new ConsultationOptionRequest(option.Label, option.LabelEn)).ToList());
        if (validation is not null) return ApiResponse<ConsultationDto>.ErrorResponse(validation);
        if (request.Options is not null && item.Participations.Count > 0)
            return ApiResponse<ConsultationDto>.ErrorResponse("Voting options cannot be changed after participation has started");

        if (request.Title is not null) item.Title = request.Title.Trim();
        if (request.TitleEn is not null) item.TitleEn = Optional(request.TitleEn);
        if (request.Description is not null) item.Description = request.Description.Trim();
        if (request.DescriptionEn is not null) item.DescriptionEn = Optional(request.DescriptionEn);
        if (request.Icon is not null) item.Icon = string.IsNullOrWhiteSpace(request.Icon) ? "ri-chat-poll-line" : request.Icon.Trim();
        if (request.LayoutType is not null) item.LayoutType = Layout(request.LayoutType);
        if (request.ActionUrl is not null) item.ActionUrl = Optional(request.ActionUrl);
        if (request.ActionLabel is not null) item.ActionLabel = Optional(request.ActionLabel);
        if (request.ActionLabelEn is not null) item.ActionLabelEn = Optional(request.ActionLabelEn);
        if (request.SecondaryActionUrl is not null) item.SecondaryActionUrl = Optional(request.SecondaryActionUrl);
        if (request.SecondaryActionLabel is not null) item.SecondaryActionLabel = Optional(request.SecondaryActionLabel);
        if (request.SecondaryActionLabelEn is not null) item.SecondaryActionLabelEn = Optional(request.SecondaryActionLabelEn);
        if (request.AccentColor is not null) item.AccentColor = Accent(request.AccentColor);
        if (request.DisplayOrder.HasValue) item.DisplayOrder = request.DisplayOrder.Value;
        if (request.IsActive.HasValue) item.IsActive = request.IsActive.Value;
        if (request.GovernanceType is not null) item.GovernanceType = Governance(request.GovernanceType);
        if (request.OpensAtUtc.HasValue) item.OpensAtUtc = request.OpensAtUtc.Value.ToUniversalTime();
        if (request.ClosesAtUtc.HasValue) item.ClosesAtUtc = request.ClosesAtUtc.Value.ToUniversalTime();
        if (request.CommentClosesAtUtc.HasValue) item.CommentClosesAtUtc = request.CommentClosesAtUtc.Value.ToUniversalTime();
        if (request.VotingMode is not null) item.VotingMode = Voting(request.VotingMode);
        if (request.EligibilityRule is not null) item.EligibilityRule = Eligibility(request.EligibilityRule);
        if (request.QuorumPercentage.HasValue) item.QuorumPercentage = Math.Clamp(request.QuorumPercentage.Value, 0, 100);
        if (request.MinimumParticipation.HasValue) item.MinimumParticipation = Math.Max(0, request.MinimumParticipation.Value);
        if (request.AllowComments.HasValue) item.AllowComments = request.AllowComments.Value;
        if (request.Options is not null)
        {
            context.ConsultationOptions.RemoveRange(item.Options);
            item.Options.Clear();
            SetOptions(item, request.Options);
        }
        item.UpdatedAt = DateTime.UtcNow;
        context.ConsultationAuditEvents.Add(new ConsultationAuditEvent { ConsultationId = item.Id, UserId = userId, Action = "Updated" });
        await context.SaveChangesAsync();
        return ApiResponse<ConsultationDto>.SuccessResponse(await MapAsync(item, null, true));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var item = await context.Consultations.FindAsync(id);
        if (item is null) return ApiResponse<bool>.ErrorResponse("Consultation not found");
        context.Consultations.Remove(item);
        await context.SaveChangesAsync();
        return ApiResponse<bool>.SuccessResponse(true);
    }

    public async Task<ApiResponse<bool>> ToggleStatusAsync(Guid id)
    {
        var item = await context.Consultations.FindAsync(id);
        if (item is null) return ApiResponse<bool>.ErrorResponse("Consultation not found");
        item.IsActive = !item.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return ApiResponse<bool>.SuccessResponse(true);
    }

    public async Task<ApiResponse<ConsultationDto>> VoteAsync(Guid id, Guid userId, CastConsultationVoteRequest request)
    {
        var item = await Query().FirstOrDefaultAsync(candidate => candidate.Id == id && candidate.IsActive);
        if (item is null) return ApiResponse<ConsultationDto>.ErrorResponse("Consultation not found");
        if (Status(item) != "Open") return ApiResponse<ConsultationDto>.ErrorResponse("Voting is not open");
        if (!await IsEligibleAsync(userId, item.EligibilityRule)) return ApiResponse<ConsultationDto>.ErrorResponse("You are not eligible to participate");
        if (!item.Options.Any(option => option.Id == request.OptionId)) return ApiResponse<ConsultationDto>.ErrorResponse("Invalid voting option");
        if (item.Participations.Any(participation => participation.UserId == userId)) return ApiResponse<ConsultationDto>.ErrorResponse("You have already participated");

        var anonymous = item.VotingMode == "Anonymous";
        context.ConsultationParticipations.Add(new ConsultationParticipation { ConsultationId = item.Id, UserId = userId });
        context.ConsultationBallots.Add(new ConsultationBallot { ConsultationId = item.Id, OptionId = request.OptionId, UserId = anonymous ? null : userId });
        context.ConsultationAuditEvents.Add(new ConsultationAuditEvent
        {
            ConsultationId = item.Id, UserId = anonymous ? null : userId, Action = "VoteCast",
            Details = anonymous ? "Anonymous ballot recorded" : "Named ballot recorded"
        });
        await context.SaveChangesAsync();
        return ApiResponse<ConsultationDto>.SuccessResponse(await MapAsync(item, userId, false));
    }

    public async Task<ApiResponse<ConsultationCommentDto>> CommentAsync(Guid id, Guid userId, AddConsultationCommentRequest request)
    {
        var item = await Query().FirstOrDefaultAsync(candidate => candidate.Id == id && candidate.IsActive);
        if (item is null) return ApiResponse<ConsultationCommentDto>.ErrorResponse("Consultation not found");
        if (!await IsEligibleAsync(userId, item.EligibilityRule)) return ApiResponse<ConsultationCommentDto>.ErrorResponse("You are not eligible to comment");
        if (!CanComment(item, DateTime.UtcNow)) return ApiResponse<ConsultationCommentDto>.ErrorResponse("The comment period is closed");
        var user = await context.Users.Include(candidate => candidate.Member).SingleAsync(candidate => candidate.Id == userId);
        var comment = new ConsultationComment { ConsultationId = id, UserId = userId, Body = request.Body.Trim(), User = user };
        context.ConsultationComments.Add(comment);
        context.ConsultationAuditEvents.Add(new ConsultationAuditEvent { ConsultationId = id, UserId = userId, Action = "CommentAdded" });
        await context.SaveChangesAsync();
        return ApiResponse<ConsultationCommentDto>.SuccessResponse(MapComment(comment));
    }

    public async Task<ApiResponse<ConsultationDto>> PublishResultsAsync(Guid id, Guid userId, bool publish)
    {
        var item = await Query().FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (item is null) return ApiResponse<ConsultationDto>.ErrorResponse("Consultation not found");
        if (publish && Status(item) != "Closed") return ApiResponse<ConsultationDto>.ErrorResponse("Results can only be published after the consultation closes");
        item.ResultsPublishedAtUtc = publish ? DateTime.UtcNow : null;
        item.UpdatedAt = DateTime.UtcNow;
        context.ConsultationAuditEvents.Add(new ConsultationAuditEvent { ConsultationId = item.Id, UserId = userId, Action = publish ? "ResultsPublished" : "ResultsUnpublished" });
        await context.SaveChangesAsync();
        return ApiResponse<ConsultationDto>.SuccessResponse(await MapAsync(item, null, true));
    }

    public async Task<ApiResponse<List<ConsultationAuditEventDto>>> GetAuditAsync(Guid id)
    {
        if (!await context.Consultations.AnyAsync(item => item.Id == id)) return ApiResponse<List<ConsultationAuditEventDto>>.ErrorResponse("Consultation not found");
        var events = await context.ConsultationAuditEvents.AsNoTracking().Include(item => item.User)
            .Where(item => item.ConsultationId == id).OrderByDescending(item => item.CreatedAtUtc).ToListAsync();
        return ApiResponse<List<ConsultationAuditEventDto>>.SuccessResponse(events.Select(item =>
            new ConsultationAuditEventDto(item.Id, item.Action, item.Details, item.User?.Email, item.CreatedAtUtc)).ToList());
    }

    private IQueryable<Consultation> Query() => context.Consultations
        .Include(item => item.Options).ThenInclude(option => option.Ballots)
        .Include(item => item.Comments).ThenInclude(comment => comment.User).ThenInclude(user => user!.Member)
        .Include(item => item.Participations).Include(item => item.Ballots).Include(item => item.AuditEvents);

    private async Task<ConsultationDto> MapAsync(Consultation item, Guid? userId, bool admin)
    {
        var eligible = userId.HasValue && await IsEligibleAsync(userId.Value, item.EligibilityRule);
        var eligibleCount = await EligibleCountAsync(item.EligibilityRule);
        var participantCount = item.Participations.Count;
        var required = Math.Max(item.MinimumParticipation, (int)Math.Ceiling(eligibleCount * item.QuorumPercentage / 100d));
        var quorumReached = required == 0 || participantCount >= required;
        var status = Status(item);
        var hasParticipated = userId.HasValue && item.Participations.Any(participation => participation.UserId == userId.Value);
        var showResults = admin || item.ResultsPublishedAtUtc.HasValue;
        var totalVotes = item.Ballots.Count;
        var results = showResults
            ? item.Options.OrderBy(option => option.DisplayOrder).Select(option =>
                new ConsultationResultDto(option.Id, option.Label, option.LabelEn, option.Ballots.Count,
                    totalVotes == 0 ? 0 : Math.Round(option.Ballots.Count * 100d / totalVotes, 1))).ToList()
            : [];
        var selectedOption = item.VotingMode == "Named" && userId.HasValue
            ? item.Ballots.FirstOrDefault(ballot => ballot.UserId == userId.Value)?.OptionId : null;
        var summary = new ConsultationGovernanceSummaryDto(
            status, eligible, hasParticipated, eligible && !hasParticipated && status == "Open" && item.Options.Count > 0,
            eligible && CanComment(item, DateTime.UtcNow), eligibleCount, participantCount, required, quorumReached,
            item.ResultsPublishedAtUtc.HasValue, results);
        return new ConsultationDto(item.Id, item.Title, item.Description, item.Icon, item.LayoutType,
            item.ActionUrl, item.ActionLabel, item.SecondaryActionUrl, item.SecondaryActionLabel,
            item.AccentColor, item.DisplayOrder, item.IsActive, item.CreatedAt, item.UpdatedAt,
            item.TitleEn, item.DescriptionEn, item.ActionLabelEn, item.SecondaryActionLabelEn,
            item.GovernanceType, item.OpensAtUtc, item.ClosesAtUtc, item.CommentClosesAtUtc,
            item.VotingMode, item.EligibilityRule, item.QuorumPercentage, item.MinimumParticipation,
            item.AllowComments, item.ResultsPublishedAtUtc,
            item.Options.OrderBy(option => option.DisplayOrder).Select(option => new ConsultationOptionDto(option.Id, option.Label, option.LabelEn, option.DisplayOrder)).ToList(),
            item.Comments.OrderByDescending(comment => comment.CreatedAtUtc).Select(MapComment).ToList(), summary, selectedOption);
    }

    private async Task<bool> IsEligibleAsync(Guid userId, string rule)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId && item.IsActive);
        if (user is null) return false;
        if (rule == "Administrators") return user.IsAdmin;
        if (user.MemberId is null) return false;
        if (rule == "AllMembers") return true;
        return await context.MembershipStandings.AsNoTracking().AnyAsync(item => item.UserId == userId &&
            (item.Status == MembershipStatuses.Active || item.Status == MembershipStatuses.GracePeriod));
    }

    private Task<int> EligibleCountAsync(string rule) => rule switch
    {
        "Administrators" => context.Users.AsNoTracking().CountAsync(item => item.IsActive && item.IsAdmin),
        "AllMembers" => context.Users.AsNoTracking().CountAsync(item => item.IsActive && item.MemberId != null),
        _ => context.MembershipStandings.AsNoTracking().CountAsync(item => item.User!.IsActive && item.User.MemberId != null &&
            (item.Status == MembershipStatuses.Active || item.Status == MembershipStatuses.GracePeriod))
    };

    private static string Status(Consultation item)
    {
        var now = DateTime.UtcNow;
        if (!item.IsActive) return "Draft";
        if (item.OpensAtUtc.HasValue && item.OpensAtUtc.Value > now) return "Upcoming";
        if (item.ClosesAtUtc.HasValue && item.ClosesAtUtc.Value <= now) return "Closed";
        return "Open";
    }

    private static bool CanComment(Consultation item, DateTime now) => item.AllowComments && item.IsActive &&
        (!item.OpensAtUtc.HasValue || item.OpensAtUtc <= now) &&
        (!item.CommentClosesAtUtc.HasValue ? !item.ClosesAtUtc.HasValue || item.ClosesAtUtc > now : item.CommentClosesAtUtc > now);

    private static ConsultationCommentDto MapComment(ConsultationComment item)
    {
        if (item.User is null)
            return new ConsultationCommentDto(item.Id, "Former member", item.Body, item.CreatedAtUtc);
        var memberName = item.User.Member is null ? $"{item.User.FirstName} {item.User.LastName}".Trim() : $"{item.User.Member.FirstName} {item.User.Member.LastName}".Trim();
        return new ConsultationCommentDto(item.Id, string.IsNullOrWhiteSpace(memberName) ? "HCBE member" : memberName, item.Body, item.CreatedAtUtc);
    }

    private static string? Validate(string type, DateTime? opens, DateTime? closes, DateTime? commentsClose, int quorum, int minimum, IReadOnlyCollection<ConsultationOptionRequest>? options)
    {
        var normalizedType = Governance(type);
        if (opens.HasValue && closes.HasValue && opens >= closes) return "Closing time must be after opening time";
        if (commentsClose.HasValue && opens.HasValue && commentsClose <= opens) return "Comment closing time must be after opening time";
        if (quorum is < 0 or > 100) return "Quorum percentage must be between 0 and 100";
        if (minimum < 0) return "Minimum participation cannot be negative";
        if (normalizedType is "Survey" or "Vote" && (options?.Count ?? 0) < 2) return "Surveys and votes require at least two options";
        if (options?.Any(option => string.IsNullOrWhiteSpace(option.Label)) == true) return "Every voting option requires a French label";
        return null;
    }

    private static void SetOptions(Consultation item, IEnumerable<ConsultationOptionRequest>? options)
    {
        if (options is null) return;
        var order = 0;
        foreach (var option in options) item.Options.Add(new ConsultationOption { Label = option.Label.Trim(), LabelEn = Optional(option.LabelEn), DisplayOrder = order++ });
    }

    private static string Governance(string? value) => GovernanceTypes.FirstOrDefault(item => item.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? "Information";
    private static string Voting(string? value) => VotingModes.FirstOrDefault(item => item.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? "Named";
    private static string Eligibility(string? value) => EligibilityRules.FirstOrDefault(item => item.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? "ActiveMembers";
    private static string Layout(string? value) => value?.Trim().Equals("featured", StringComparison.OrdinalIgnoreCase) == true ? "featured" : "card";
    private static string Accent(string? value) => value?.Trim().Equals("amber", StringComparison.OrdinalIgnoreCase) == true ? "amber" : "emerald";
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
