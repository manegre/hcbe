using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using System.Net.Mail;

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

    public async Task<ApiResponse<MemberImportResultDto>> ImportAsync(MemberImportRequest request)
    {
        if (request.Rows.Count == 0 || request.Rows.Count > 2000)
            return ApiResponse<MemberImportResultDto>.ErrorResponse("The import must contain between 1 and 2,000 rows");

        var existingEmails = (await _context.Members.AsNoTracking().Select(item => item.Email).ToListAsync())
            .Select(NormalizeEmail).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var batchEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<MemberImportRowResultDto>(request.Rows.Count);
        var accepted = new List<MemberImportRowDto>();
        foreach (var row in request.Rows)
        {
            var issues = new List<string>();
            var email = NormalizeEmail(row.Email);
            if (string.IsNullOrWhiteSpace(row.FirstName)) issues.Add("First name is required");
            if (string.IsNullOrWhiteSpace(row.LastName)) issues.Add("Last name is required");
            if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out _)) issues.Add("A valid email is required");
            var duplicate = !string.IsNullOrWhiteSpace(email) && (existingEmails.Contains(email) || !batchEmails.Add(email));
            if (duplicate) issues.Add("Email already exists");
            var status = duplicate ? "Duplicate" : issues.Count > 0 ? "Invalid" : "Ready";
            if (status == "Ready") accepted.Add(row);
            results.Add(new(row.RowNumber, $"{row.FirstName} {row.LastName}".Trim(), row.Email?.Trim(), status, issues));
        }

        var preview = new MemberImportPreviewDto(results.Count, results.Count(item => item.Status == "Ready"), results.Count(item => item.Status == "Ready"), results.Count(item => item.Status == "Duplicate"), results.Count(item => item.Status == "Invalid"), results);
        if (!request.Commit) return ApiResponse<MemberImportResultDto>.SuccessResponse(new(preview, 0));

        foreach (var row in accepted)
        {
            _context.Members.Add(new Member
            {
                FirstName = row.FirstName!.Trim(), LastName = row.LastName!.Trim(), Email = row.Email!.Trim(),
                Phone = Clean(row.Phone), City = Clean(row.City), Province = Clean(row.Province), Profession = Clean(row.Profession),
                Expertise = Clean(row.Expertise), Interests = Clean(row.Interests), Availability = Clean(row.Availability), Zone = Clean(row.Zone), CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
        return ApiResponse<MemberImportResultDto>.SuccessResponse(new(preview, accepted.Count));
    }

    public async Task<ApiResponse<List<MemberDuplicateCandidateDto>>> FindDuplicatesAsync()
    {
        var members = await _context.Members.AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync();
        var candidates = new List<MemberDuplicateCandidateDto>();
        for (var i = 0; i < members.Count; i++)
        for (var j = i + 1; j < members.Count; j++)
        {
            var reasons = new List<string>(); var score = 0;
            if (NormalizeEmail(members[i].Email) == NormalizeEmail(members[j].Email)) { score += 100; reasons.Add("Same email"); }
            if (!string.IsNullOrWhiteSpace(NormalizePhone(members[i].Phone)) && NormalizePhone(members[i].Phone) == NormalizePhone(members[j].Phone)) { score += 70; reasons.Add("Same phone"); }
            if (NormalizeText($"{members[i].FirstName} {members[i].LastName}") == NormalizeText($"{members[j].FirstName} {members[j].LastName}")) { score += 45; reasons.Add("Same name"); }
            if (!string.IsNullOrWhiteSpace(members[i].City) && NormalizeText(members[i].City) == NormalizeText(members[j].City)) { score += 15; reasons.Add("Same city"); }
            if (score >= 60) candidates.Add(new(MapToDto(members[i]), MapToDto(members[j]), Math.Min(score, 100), reasons));
        }
        return ApiResponse<List<MemberDuplicateCandidateDto>>.SuccessResponse(candidates.OrderByDescending(item => item.Score).Take(250).ToList());
    }

    public async Task<ApiResponse<MemberDto>> MergeAsync(Guid primaryMemberId, Guid duplicateMemberId)
    {
        if (primaryMemberId == duplicateMemberId) return ApiResponse<MemberDto>.ErrorResponse("Choose two different members");
        var primary = await _context.Members.FindAsync(primaryMemberId); var duplicate = await _context.Members.FindAsync(duplicateMemberId);
        if (primary is null || duplicate is null) return ApiResponse<MemberDto>.ErrorResponse("Member not found");
        var linkedUsers = await _context.Users.Where(item => item.MemberId == primaryMemberId || item.MemberId == duplicateMemberId).ToListAsync();
        if (linkedUsers.Count > 1) return ApiResponse<MemberDto>.ErrorResponse("Both records have user accounts. Resolve the account access before merging.");
        if (await _context.PrivateConversations.AnyAsync(item =>
            (item.MemberOneId == primaryMemberId && item.MemberTwoId == duplicateMemberId) ||
            (item.MemberOneId == duplicateMemberId && item.MemberTwoId == primaryMemberId)))
            return ApiResponse<MemberDto>.ErrorResponse("These members have a direct private conversation. Preserve or close that conversation before merging.");

        await using var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            primary.Phone = Prefer(primary.Phone, duplicate.Phone); primary.City = Prefer(primary.City, duplicate.City); primary.Province = Prefer(primary.Province, duplicate.Province);
            primary.Profession = Prefer(primary.Profession, duplicate.Profession); primary.Expertise = Prefer(primary.Expertise, duplicate.Expertise); primary.Interests = Prefer(primary.Interests, duplicate.Interests);
            primary.Availability = Prefer(primary.Availability, duplicate.Availability); primary.Zone = Prefer(primary.Zone, duplicate.Zone); primary.IsAdmin |= duplicate.IsAdmin;
            if (linkedUsers.SingleOrDefault()?.MemberId == duplicateMemberId) linkedUsers[0].MemberId = primaryMemberId;
            foreach (var item in await _context.MembershipApplications.Where(x => x.MemberId == duplicateMemberId).ToListAsync()) item.MemberId = primaryMemberId;
            foreach (var item in await _context.ServiceCases.Where(x => x.MemberId == duplicateMemberId).ToListAsync()) item.MemberId = primaryMemberId;
            foreach (var item in await _context.Associations.Where(x => x.OwnerMemberId == duplicateMemberId).ToListAsync()) item.OwnerMemberId = primaryMemberId;
            foreach (var item in await _context.AssociationClaimRequests.Where(x => x.MemberId == duplicateMemberId).ToListAsync()) item.MemberId = primaryMemberId;
            foreach (var item in await _context.AssociationJoinRequests.Where(x => x.MemberId == duplicateMemberId).ToListAsync()) item.MemberId = primaryMemberId;
            foreach (var item in await _context.MentorshipApplications.Where(x => x.MemberId == duplicateMemberId).ToListAsync()) item.MemberId = primaryMemberId;
            foreach (var item in await _context.MentorshipGoals.Where(x => x.CreatedByMemberId == duplicateMemberId).ToListAsync()) item.CreatedByMemberId = primaryMemberId;
            foreach (var item in await _context.MentorshipCheckIns.Where(x => x.MemberId == duplicateMemberId).ToListAsync()) item.MemberId = primaryMemberId;

            await MergeUniqueChildren(primaryMemberId, duplicateMemberId);
            await MergeNetworking(primaryMemberId, duplicateMemberId);
            await MergeRelationships(primaryMemberId, duplicateMemberId);
            _context.Members.Remove(duplicate);
            await _context.SaveChangesAsync(); if (transaction is not null) await transaction.CommitAsync();
            return ApiResponse<MemberDto>.SuccessResponse(MapToDto(primary));
        }
        catch (Exception ex)
        {
            if (transaction is not null) await transaction.RollbackAsync();
            return ApiResponse<MemberDto>.ErrorResponse("The member records could not be merged safely", new() { ex.Message });
        }
    }

    private async Task MergeUniqueChildren(Guid primaryId, Guid duplicateId)
    {
        foreach (var item in await _context.EventRegistrations.Include(x => x.SurveyResponse).Where(x => x.MemberId == duplicateId).ToListAsync())
        {
            var existing = await _context.EventRegistrations.Include(x => x.SurveyResponse).SingleOrDefaultAsync(x => x.EventId == item.EventId && x.MemberId == primaryId);
            if (existing is null) { item.MemberId = primaryId; continue; }
            existing.AccessibilityNeeds = Prefer(existing.AccessibilityNeeds, item.AccessibilityNeeds); existing.AdminNotes = Prefer(existing.AdminNotes, item.AdminNotes);
            existing.CheckedInAt ??= item.CheckedInAt; existing.ReminderSentAt ??= item.ReminderSentAt; existing.RegisteredAt = existing.RegisteredAt < item.RegisteredAt ? existing.RegisteredAt : item.RegisteredAt;
            if (item.SurveyResponse is not null && existing.SurveyResponse is null) { var survey = item.SurveyResponse; item.SurveyResponse = null; survey.EventRegistrationId = existing.Id; survey.EventRegistration = existing; existing.SurveyResponse = survey; }
            else if (item.SurveyResponse is not null && existing.SurveyResponse is not null) { existing.SurveyResponse.Feedback = Prefer(existing.SurveyResponse.Feedback, item.SurveyResponse.Feedback); existing.SurveyResponse.ConsentToQuote |= item.SurveyResponse.ConsentToQuote; existing.SurveyResponse.Rating = Math.Max(existing.SurveyResponse.Rating, item.SurveyResponse.Rating); _context.EventSurveyResponses.Remove(item.SurveyResponse); }
            _context.EventRegistrations.Remove(item);
        }
        foreach (var item in await _context.AssociationMembers.Where(x => x.MemberId == duplicateId).ToListAsync())
        {
            var existing = await _context.AssociationMembers.SingleOrDefaultAsync(x => x.AssociationId == item.AssociationId && x.MemberId == primaryId);
            if (existing is null) item.MemberId = primaryId; else { existing.Title = Prefer(existing.Title, item.Title); existing.Status = existing.Status == "Active" || item.Status == "Active" ? "Active" : existing.Status; existing.Permissions = string.Join(',', existing.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Concat(item.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Distinct(StringComparer.OrdinalIgnoreCase)); _context.AssociationMembers.Remove(item); }
        }
        foreach (var item in await _context.OpportunityApplications.Include(x => x.Documents).Include(x => x.VolunteerTimeEntries).Include(x => x.Certificate).Where(x => x.MemberId == duplicateId).ToListAsync())
        {
            var existing = await _context.OpportunityApplications.Include(x => x.Certificate).SingleOrDefaultAsync(x => x.OpportunityId == item.OpportunityId && x.MemberId == primaryId);
            if (existing is null) { item.MemberId = primaryId; continue; }
            existing.Message = Prefer(existing.Message, item.Message) ?? ""; existing.Experience = Prefer(existing.Experience, item.Experience); existing.Availability = Prefer(existing.Availability, item.Availability); existing.MatchScore = Math.Max(existing.MatchScore, item.MatchScore); existing.MatchReasons = Prefer(existing.MatchReasons, item.MatchReasons); existing.AdminNotes = Prefer(existing.AdminNotes, item.AdminNotes);
            foreach (var document in item.Documents.ToList()) { item.Documents.Remove(document); document.OpportunityApplicationId = existing.Id; document.OpportunityApplication = existing; existing.Documents.Add(document); }
            foreach (var timeEntry in item.VolunteerTimeEntries.ToList()) { item.VolunteerTimeEntries.Remove(timeEntry); timeEntry.OpportunityApplicationId = existing.Id; timeEntry.OpportunityApplication = existing; existing.VolunteerTimeEntries.Add(timeEntry); }
            if (item.Certificate is not null && existing.Certificate is null) { var certificate = item.Certificate; item.Certificate = null; certificate.OpportunityApplicationId = existing.Id; certificate.OpportunityApplication = existing; existing.Certificate = certificate; }
            else if (item.Certificate is not null && existing.Certificate is not null) { existing.Certificate.ContributionSummary = Prefer(existing.Certificate.ContributionSummary, item.Certificate.ContributionSummary); existing.Certificate.ConfirmedHours = Math.Max(existing.Certificate.ConfirmedHours ?? 0, item.Certificate.ConfirmedHours ?? 0); _context.OpportunityCertificates.Remove(item.Certificate); }
            _context.OpportunityApplications.Remove(item);
        }
    }

    private async Task MergeNetworking(Guid primaryId, Guid duplicateId)
    {
        var primaryProfile = await _context.NetworkingProfiles.SingleOrDefaultAsync(x => x.MemberId == primaryId);
        var duplicateProfile = await _context.NetworkingProfiles.SingleOrDefaultAsync(x => x.MemberId == duplicateId);
        if (duplicateProfile is not null && primaryProfile is null) duplicateProfile.MemberId = primaryId;
        else if (duplicateProfile is not null && primaryProfile is not null) { primaryProfile.Headline = Prefer(primaryProfile.Headline, duplicateProfile.Headline) ?? ""; primaryProfile.Bio = Prefer(primaryProfile.Bio, duplicateProfile.Bio) ?? ""; primaryProfile.Expertise = Prefer(primaryProfile.Expertise, duplicateProfile.Expertise) ?? ""; primaryProfile.Sectors = Prefer(primaryProfile.Sectors, duplicateProfile.Sectors) ?? ""; primaryProfile.IsVisible |= duplicateProfile.IsVisible; primaryProfile.AllowContactRequests |= duplicateProfile.AllowContactRequests; _context.NetworkingProfiles.Remove(duplicateProfile); }
        foreach (var item in await _context.ConnectionRequests.Where(x => x.RequesterMemberId == duplicateId || x.RecipientMemberId == duplicateId).ToListAsync()) { if ((item.RequesterMemberId == duplicateId ? primaryId : item.RequesterMemberId) == (item.RecipientMemberId == duplicateId ? primaryId : item.RecipientMemberId)) _context.ConnectionRequests.Remove(item); else { if (item.RequesterMemberId == duplicateId) item.RequesterMemberId = primaryId; if (item.RecipientMemberId == duplicateId) item.RecipientMemberId = primaryId; } }
    }

    private async Task MergeRelationships(Guid primaryId, Guid duplicateId)
    {
        foreach (var item in await _context.PrivateMessages.Where(x => x.SenderMemberId == duplicateId).ToListAsync()) item.SenderMemberId = primaryId;
        foreach (var item in await _context.ConversationReports.Where(x => x.ReporterMemberId == duplicateId).ToListAsync()) item.ReporterMemberId = primaryId;
        foreach (var conversation in await _context.PrivateConversations.Where(x => x.MemberOneId == duplicateId || x.MemberTwoId == duplicateId).ToListAsync())
        {
            var first = conversation.MemberOneId == duplicateId ? primaryId : conversation.MemberOneId; var second = conversation.MemberTwoId == duplicateId ? primaryId : conversation.MemberTwoId;
            if (first == second) { _context.PrivateConversations.Remove(conversation); continue; }
            if (first.CompareTo(second) > 0) (first, second) = (second, first);
            var existing = await _context.PrivateConversations.FirstOrDefaultAsync(x => x.Id != conversation.Id && x.MemberOneId == first && x.MemberTwoId == second);
            if (existing is not null) { foreach (var message in await _context.PrivateMessages.Where(x => x.ConversationId == conversation.Id).ToListAsync()) message.ConversationId = existing.Id; foreach (var report in await _context.ConversationReports.Where(x => x.ConversationId == conversation.Id).ToListAsync()) report.ConversationId = existing.Id; _context.PrivateConversations.Remove(conversation); }
            else { conversation.MemberOneId = first; conversation.MemberTwoId = second; }
        }
        foreach (var block in await _context.MemberBlocks.Where(x => x.BlockerMemberId == duplicateId || x.BlockedMemberId == duplicateId).ToListAsync())
        {
            var blocker = block.BlockerMemberId == duplicateId ? primaryId : block.BlockerMemberId; var blocked = block.BlockedMemberId == duplicateId ? primaryId : block.BlockedMemberId;
            if (blocker == blocked || await _context.MemberBlocks.AnyAsync(x => x.Id != block.Id && x.BlockerMemberId == blocker && x.BlockedMemberId == blocked)) _context.MemberBlocks.Remove(block); else { block.BlockerMemberId = blocker; block.BlockedMemberId = blocked; }
        }
    }

    private static string NormalizeEmail(string? value) => value?.Trim().ToLowerInvariant() ?? "";
    private static string NormalizePhone(string? value) => new((value ?? "").Where(char.IsDigit).ToArray());
    private static string NormalizeText(string? value) => string.Concat((value ?? "").Normalize(System.Text.NormalizationForm.FormD).Where(character => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)).Trim().ToLowerInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? Prefer(string? primary, string? duplicate) => string.IsNullOrWhiteSpace(primary) ? Clean(duplicate) : primary;

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

