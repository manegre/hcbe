using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public class MessagingService : IMessagingService
{
    private readonly ApplicationDbContext _context;

    public MessagingService(ApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<List<MessagingContactDto>>> GetEligibleContactsAsync(Guid userId)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<MessagingContactDto>>.ErrorResponse("Member profile required");
        var relationships = await GetRelationshipsAsync(memberId.Value);
        var conversations = await _context.PrivateConversations.AsNoTracking()
            .Where(x => x.MemberOneId == memberId || x.MemberTwoId == memberId).ToListAsync();
        var contacts = relationships.Select(item =>
        {
            var conversation = conversations.FirstOrDefault(x => x.MemberOneId == item.MemberId || x.MemberTwoId == item.MemberId);
            return new MessagingContactDto(item.MemberId, item.MemberName, item.Type, item.RelationshipId, conversation is not null, conversation?.Id);
        }).OrderBy(x => x.MemberName).ToList();
        return ApiResponse<List<MessagingContactDto>>.SuccessResponse(contacts);
    }

    public async Task<ApiResponse<List<ConversationDto>>> GetConversationsAsync(Guid userId)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<ConversationDto>>.ErrorResponse("Member profile required");
        var conversations = await ConversationsQuery().Where(x => x.MemberOneId == memberId || x.MemberTwoId == memberId)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt).ToListAsync();
        var result = new List<ConversationDto>();
        foreach (var conversation in conversations) result.Add(await MapConversationAsync(conversation, memberId.Value));
        return ApiResponse<List<ConversationDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<ConversationDto>> StartConversationAsync(Guid userId, StartConversationRequest request)
    {
        var memberId = await GetMemberIdAsync(userId);
        if (memberId is null) return ApiResponse<ConversationDto>.ErrorResponse("Member profile required");
        if (memberId == request.MemberId) return ApiResponse<ConversationDto>.ErrorResponse("You cannot message yourself");
        var relationships = await GetRelationshipsAsync(memberId.Value);
        var relationship = relationships.FirstOrDefault(x => x.MemberId == request.MemberId);
        if (relationship is null) return ApiResponse<ConversationDto>.ErrorResponse("An accepted connection or active mentorship match is required");
        var first = memberId.Value.CompareTo(request.MemberId) < 0 ? memberId.Value : request.MemberId;
        var second = first == memberId.Value ? request.MemberId : memberId.Value;
        var existing = await ConversationsQuery(false).FirstOrDefaultAsync(x => x.MemberOneId == first && x.MemberTwoId == second);
        if (existing is not null) return ApiResponse<ConversationDto>.SuccessResponse(await MapConversationAsync(existing, memberId.Value));
        var conversation = new PrivateConversation
        {
            MemberOneId = first, MemberTwoId = second,
            RelationshipType = relationship.Type, RelationshipId = relationship.RelationshipId,
        };
        _context.PrivateConversations.Add(conversation);
        await _context.SaveChangesAsync();
        conversation = await ConversationsQuery(false).FirstAsync(x => x.Id == conversation.Id);
        return ApiResponse<ConversationDto>.SuccessResponse(await MapConversationAsync(conversation, memberId.Value));
    }

    public async Task<ApiResponse<List<PrivateMessageDto>>> GetMessagesAsync(Guid userId, Guid conversationId)
    {
        var memberId = await GetMemberIdAsync(userId);
        var conversation = memberId is null ? null : await FindForMemberAsync(conversationId, memberId.Value);
        if (conversation is null) return ApiResponse<List<PrivateMessageDto>>.ErrorResponse("Conversation not found");
        var messages = await _context.PrivateMessages.AsNoTracking().Include(x => x.SenderMember)
            .Where(x => x.ConversationId == conversationId).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
        messages.Reverse();
        return ApiResponse<List<PrivateMessageDto>>.SuccessResponse(messages.Select(x => MapMessage(x, memberId!.Value)).ToList());
    }

    public async Task<ApiResponse<PrivateMessageDto>> SendMessageAsync(Guid userId, Guid conversationId, SendPrivateMessageRequest request)
    {
        var memberId = await GetMemberIdAsync(userId);
        var conversation = memberId is null ? null : await FindForMemberAsync(conversationId, memberId.Value, false);
        if (conversation is null) return ApiResponse<PrivateMessageDto>.ErrorResponse("Conversation not found");
        if (conversation.Status != "Active") return ApiResponse<PrivateMessageDto>.ErrorResponse("This conversation is suspended");
        var body = request.Body.Trim();
        if (body.Length == 0) return ApiResponse<PrivateMessageDto>.ErrorResponse("Message cannot be empty");
        var message = new PrivateMessage { ConversationId = conversationId, SenderMemberId = memberId!.Value, Body = body };
        _context.PrivateMessages.Add(message);
        conversation.LastMessageAt = message.CreatedAt; conversation.UpdatedAt = message.CreatedAt;
        await _context.SaveChangesAsync();
        message.SenderMember = await _context.Members.FindAsync(memberId.Value);
        return ApiResponse<PrivateMessageDto>.SuccessResponse(MapMessage(message, memberId.Value));
    }

    public async Task<ApiResponse> MarkConversationReadAsync(Guid userId, Guid conversationId)
    {
        var memberId = await GetMemberIdAsync(userId);
        var conversation = memberId is null ? null : await FindForMemberAsync(conversationId, memberId.Value);
        if (conversation is null) return ApiResponse.CreateError("Conversation not found");
        var unread = await _context.PrivateMessages.Where(x => x.ConversationId == conversationId && x.SenderMemberId != memberId && x.ReadAt == null).ToListAsync();
        var now = DateTime.UtcNow;
        unread.ForEach(x => x.ReadAt = now);
        await _context.SaveChangesAsync();
        return ApiResponse.CreateSuccess("Conversation marked as read");
    }

    public async Task<ApiResponse<ConversationReportDto>> ReportConversationAsync(Guid userId, Guid conversationId, ReportConversationRequest request)
    {
        var memberId = await GetMemberIdAsync(userId);
        var conversation = memberId is null ? null : await FindForMemberAsync(conversationId, memberId.Value, false);
        if (conversation is null) return ApiResponse<ConversationReportDto>.ErrorResponse("Conversation not found");
        if (await _context.ConversationReports.AnyAsync(x => x.ConversationId == conversationId && x.ReporterMemberId == memberId && x.Status == "Open"))
            return ApiResponse<ConversationReportDto>.ErrorResponse("An open report already exists for this conversation");
        var report = new ConversationReport { ConversationId = conversationId, Conversation = conversation, ReporterMemberId = memberId!.Value, Reason = request.Reason.Trim() };
        _context.ConversationReports.Add(report);
        await _context.SaveChangesAsync();
        report.ReporterMember = await _context.Members.FindAsync(memberId.Value);
        return ApiResponse<ConversationReportDto>.SuccessResponse(MapReport(report));
    }

    public async Task<ApiResponse<List<ConversationReportDto>>> GetReportsForAdminAsync(string? status)
    {
        var query = ReportsQuery();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        var reports = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return ApiResponse<List<ConversationReportDto>>.SuccessResponse(reports.Select(MapReport).ToList());
    }

    public async Task<ApiResponse<ConversationReportDto>> ResolveReportAsync(Guid id, ResolveConversationReportRequest request)
    {
        var status = request.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) ? "Resolved" : request.Status.Equals("Dismissed", StringComparison.OrdinalIgnoreCase) ? "Dismissed" : null;
        if (status is null) return ApiResponse<ConversationReportDto>.ErrorResponse("Status must be Resolved or Dismissed");
        var report = await ReportsQuery(false).FirstOrDefaultAsync(x => x.Id == id);
        if (report is null) return ApiResponse<ConversationReportDto>.ErrorResponse("Report not found");
        report.Status = status; report.AdminNotes = Normalize(request.AdminNotes); report.ResolvedAt = DateTime.UtcNow;
        if (request.SuspendConversation) report.Conversation!.Status = "Suspended";
        await _context.SaveChangesAsync();
        return ApiResponse<ConversationReportDto>.SuccessResponse(MapReport(report));
    }

    private async Task<List<Relationship>> GetRelationshipsAsync(Guid memberId)
    {
        var results = new Dictionary<Guid, Relationship>();
        var connections = await _context.ConnectionRequests.AsNoTracking().Include(x => x.RequesterMember).Include(x => x.RecipientMember)
            .Where(x => x.Status == "Accepted" && (x.RequesterMemberId == memberId || x.RecipientMemberId == memberId)).ToListAsync();
        foreach (var item in connections)
        {
            var other = item.RequesterMemberId == memberId ? item.RecipientMember : item.RequesterMember;
            if (other is not null) results[other.Id] = new Relationship(other.Id, Name(other), "Networking", item.Id);
        }
        var matches = await _context.MentorshipMatches.AsNoTracking()
            .Include(x => x.MentorApplication).ThenInclude(x => x!.Member)
            .Include(x => x.MenteeApplication).ThenInclude(x => x!.Member)
            .Where(x => x.Status == "Active" && (x.MentorApplication!.MemberId == memberId || x.MenteeApplication!.MemberId == memberId)).ToListAsync();
        foreach (var item in matches)
        {
            var other = item.MentorApplication!.MemberId == memberId ? item.MenteeApplication!.Member : item.MentorApplication.Member;
            if (other is not null && !results.ContainsKey(other.Id)) results[other.Id] = new Relationship(other.Id, Name(other), "Mentorship", item.Id);
        }
        return results.Values.ToList();
    }

    private Task<Guid?> GetMemberIdAsync(Guid userId) => _context.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.MemberId).FirstOrDefaultAsync();
    private IQueryable<PrivateConversation> ConversationsQuery(bool noTracking = true)
    {
        var query = _context.PrivateConversations.Include(x => x.MemberOne).Include(x => x.MemberTwo).AsQueryable();
        return noTracking ? query.AsNoTracking() : query;
    }
    private Task<PrivateConversation?> FindForMemberAsync(Guid conversationId, Guid memberId, bool noTracking = true) =>
        ConversationsQuery(noTracking).FirstOrDefaultAsync(x => x.Id == conversationId && (x.MemberOneId == memberId || x.MemberTwoId == memberId));
    private IQueryable<ConversationReport> ReportsQuery(bool noTracking = true)
    {
        var query = _context.ConversationReports.Include(x => x.ReporterMember)
            .Include(x => x.Conversation).ThenInclude(x => x!.MemberOne)
            .Include(x => x.Conversation).ThenInclude(x => x!.MemberTwo).AsQueryable();
        return noTracking ? query.AsNoTracking() : query;
    }
    private async Task<ConversationDto> MapConversationAsync(PrivateConversation item, Guid viewer)
    {
        var counterpart = item.MemberOneId == viewer ? item.MemberTwo : item.MemberOne;
        var last = await _context.PrivateMessages.AsNoTracking().Where(x => x.ConversationId == item.Id).OrderByDescending(x => x.CreatedAt).Select(x => x.Body).FirstOrDefaultAsync();
        var unread = await _context.PrivateMessages.AsNoTracking().CountAsync(x => x.ConversationId == item.Id && x.SenderMemberId != viewer && x.ReadAt == null);
        return new ConversationDto(item.Id, counterpart!.Id, Name(counterpart), item.RelationshipType, item.Status, last, item.LastMessageAt, unread, item.CreatedAt);
    }
    private static PrivateMessageDto MapMessage(PrivateMessage item, Guid viewer) => new(item.Id, item.ConversationId, item.SenderMemberId, Name(item.SenderMember), item.Body, item.SenderMemberId == viewer, item.CreatedAt, item.ReadAt);
    private static ConversationReportDto MapReport(ConversationReport item) => new(item.Id, item.ConversationId, item.ReporterMemberId, Name(item.ReporterMember), Name(item.Conversation?.MemberOne), Name(item.Conversation?.MemberTwo), item.Reason, item.Status, item.AdminNotes, item.CreatedAt, item.ResolvedAt);
    private static string Name(Member? member) => member is null ? "" : $"{member.FirstName} {member.LastName}".Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private sealed record Relationship(Guid MemberId, string MemberName, string Type, Guid RelationshipId);
}
