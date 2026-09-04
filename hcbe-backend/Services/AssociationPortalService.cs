using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class AssociationPortalService(
    ApplicationDbContext context,
    INotificationService notifications,
    IFileStorageService fileStorage) : IAssociationPortalService
{
    private const string WorkspaceView = "workspace.view";
    private const string ProfileManage = "profile.manage";
    private const string MembersManage = "members.manage";
    private const string DocumentsManage = "documents.manage";
    private const string CalendarManage = "calendar.manage";
    private const string ServiceCasesManage = "service-cases.manage";
    private static readonly string[] AllPermissions = [WorkspaceView, ProfileManage, MembersManage, DocumentsManage, CalendarManage, ServiceCasesManage];
    private static readonly HashSet<string> Roles = new(["Owner", "Manager", "Editor", "Member"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> MemberStatuses = new(["Active", "Inactive"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> JoinStatuses = new(["Approved", "Rejected"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> CaseStatuses = new(["Submitted", "InReview", "AwaitingMember", "Resolved", "Closed"], StringComparer.OrdinalIgnoreCase);

    public async Task<ApiResponse<List<AssociationClaimDto>>> GetMineAsync(Guid userId)
    {
        var memberId = await MemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<AssociationClaimDto>>.ErrorResponse("Member account not found");
        var items = await Claims().Where(item => item.MemberId == memberId).OrderByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<AssociationClaimDto>>.SuccessResponse(items.Select(MapClaim).ToList());
    }

    public async Task<ApiResponse<AssociationClaimDto>> ClaimAsync(Guid userId, Guid associationId, CreateAssociationClaimRequest request)
    {
        var memberId = await MemberIdAsync(userId);
        if (memberId is null) return ApiResponse<AssociationClaimDto>.ErrorResponse("Member account not found");
        var association = await context.Associations.FindAsync(associationId);
        if (association is null || !association.IsActive) return ApiResponse<AssociationClaimDto>.ErrorResponse("Organization not found");
        if (association.OwnerMemberId is not null) return ApiResponse<AssociationClaimDto>.ErrorResponse("This organization already has an approved manager");
        var existing = await context.AssociationClaimRequests.Include(item => item.Association).Include(item => item.Member)
            .FirstOrDefaultAsync(item => item.AssociationId == associationId && item.MemberId == memberId && item.Status == "Pending");
        if (existing is not null) return ApiResponse<AssociationClaimDto>.SuccessResponse(MapClaim(existing));
        var item = new AssociationClaimRequest { AssociationId = associationId, MemberId = memberId.Value, Message = request.Message.Trim() };
        context.AssociationClaimRequests.Add(item);
        await context.SaveChangesAsync();
        item.Association = association;
        item.Member = await context.Members.FindAsync(memberId.Value);
        await notifications.CreateNotificationAsync("association-claim", "Nouvelle demande d’organisation", association.Name, item.Id, "/admin/association-requests");
        return ApiResponse<AssociationClaimDto>.SuccessResponse(MapClaim(item));
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetManagedAsync(Guid userId)
    {
        var memberId = await MemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<AssociationDto>>.SuccessResponse([]);
        var memberOrganizationIds = context.AssociationMembers.AsNoTracking()
            .Where(item => item.MemberId == memberId && item.Status == "Active").Select(item => item.AssociationId);
        var items = await context.Associations.AsNoTracking()
            .Where(item => item.IsActive && (item.OwnerMemberId == memberId || memberOrganizationIds.Contains(item.Id)))
            .OrderBy(item => item.Name).ToListAsync();
        return ApiResponse<List<AssociationDto>>.SuccessResponse(items.Select(MapAssociation).ToList());
    }

    public async Task<ApiResponse<AssociationDto>> UpdateManagedAsync(Guid userId, Guid associationId, UpdateAssociationRequest request)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(ProfileManage)) return ApiResponse<AssociationDto>.ErrorResponse("Organization profile access denied");
        var item = access.Association;
        if (request.Name is not null) item.Name = request.Name.Trim();
        if (request.NameEn is not null) item.NameEn = Normalize(request.NameEn);
        if (request.Description is not null) item.Description = Normalize(request.Description);
        if (request.DescriptionEn is not null) item.DescriptionEn = Normalize(request.DescriptionEn);
        if (request.Province is not null) item.Province = request.Province.Trim();
        if (request.City is not null) item.City = request.City.Trim();
        if (request.Contact is not null) item.Contact = Normalize(request.Contact);
        if (request.Phone is not null) item.Phone = Normalize(request.Phone);
        if (request.President is not null) item.President = Normalize(request.President);
        if (request.MemberCount is not null) item.MemberCount = Normalize(request.MemberCount);
        if (request.FoundedYear.HasValue) item.FoundedYear = request.FoundedYear;
        if (request.Website is not null) item.Website = Normalize(request.Website);
        if (request.Domains is not null) item.Domains = request.Domains;
        if (request.DomainsEn is not null) item.DomainsEn = request.DomainsEn;
        item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return ApiResponse<AssociationDto>.SuccessResponse(MapAssociation(item));
    }

    public async Task<ApiResponse<AssociationJoinRequestDto>> JoinAsync(Guid userId, Guid associationId, CreateAssociationJoinRequest request)
    {
        var memberId = await MemberIdAsync(userId);
        if (memberId is null) return ApiResponse<AssociationJoinRequestDto>.ErrorResponse("Member account not found");
        var association = await context.Associations.AsNoTracking().FirstOrDefaultAsync(item => item.Id == associationId && item.IsActive);
        if (association is null) return ApiResponse<AssociationJoinRequestDto>.ErrorResponse("Organization not found");
        if (await context.AssociationMembers.AnyAsync(item => item.AssociationId == associationId && item.MemberId == memberId && item.Status == "Active") || association.OwnerMemberId == memberId)
            return ApiResponse<AssociationJoinRequestDto>.ErrorResponse("You already belong to this organization");
        var existing = await JoinRequests().FirstOrDefaultAsync(item => item.AssociationId == associationId && item.MemberId == memberId && item.Status == "Pending");
        if (existing is not null) return ApiResponse<AssociationJoinRequestDto>.SuccessResponse(MapJoin(existing));
        var item = new AssociationJoinRequest { AssociationId = associationId, MemberId = memberId.Value, Message = request.Message.Trim() };
        context.AssociationJoinRequests.Add(item);
        await context.SaveChangesAsync();
        item.Member = await context.Members.FindAsync(memberId.Value);
        await notifications.CreateNotificationAsync("association-join", "Nouvelle demande d’adhésion", association.Name, item.Id, $"/admin/associations/{associationId}");
        return ApiResponse<AssociationJoinRequestDto>.SuccessResponse(MapJoin(item));
    }

    public async Task<ApiResponse<List<AssociationJoinRequestDto>>> GetMyJoinRequestsAsync(Guid userId)
    {
        var memberId = await MemberIdAsync(userId);
        if (memberId is null) return ApiResponse<List<AssociationJoinRequestDto>>.ErrorResponse("Member account not found");
        var items = await JoinRequests().Where(item => item.MemberId == memberId)
            .OrderByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<AssociationJoinRequestDto>>.SuccessResponse(items.Select(MapJoin).ToList());
    }

    public async Task<ApiResponse<AssociationWorkspaceDto>> GetWorkspaceAsync(Guid userId, Guid associationId)
    {
        var access = await AccessAsync(userId, associationId);
        return access is null
            ? ApiResponse<AssociationWorkspaceDto>.ErrorResponse("Organization workspace access denied")
            : ApiResponse<AssociationWorkspaceDto>.SuccessResponse(await BuildWorkspaceAsync(access));
    }

    public async Task<ApiResponse<AssociationJoinRequestDto>> ReviewJoinAsync(Guid userId, Guid associationId, Guid requestId, ReviewAssociationJoinRequest request)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(MembersManage)) return ApiResponse<AssociationJoinRequestDto>.ErrorResponse("Member management access denied");
        return await ReviewJoinCoreAsync(userId, associationId, requestId, request);
    }

    public async Task<ApiResponse<AssociationMemberDto>> UpdateMemberAsync(Guid userId, Guid associationId, Guid associationMemberId, UpdateAssociationMemberRequest request)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(MembersManage)) return ApiResponse<AssociationMemberDto>.ErrorResponse("Member management access denied");
        return await UpdateMemberCoreAsync(associationId, associationMemberId, request);
    }

    public async Task<ApiResponse> RemoveMemberAsync(Guid userId, Guid associationId, Guid associationMemberId)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(MembersManage)) return ApiResponse.CreateError("Member management access denied");
        return await RemoveMemberCoreAsync(associationId, associationMemberId);
    }

    public async Task<ApiResponse<AssociationDocumentDto>> AddDocumentAsync(Guid userId, Guid associationId, IFormFile file, CreateAssociationDocumentRequest request)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(DocumentsManage)) return ApiResponse<AssociationDocumentDto>.ErrorResponse("Document management access denied");
        if (!fileStorage.IsAllowedExtension(file.FileName)) return ApiResponse<AssociationDocumentDto>.ErrorResponse("File type not allowed");
        var saved = await fileStorage.SaveAsync(file, $"organizations/{associationId:N}");
        var item = new AssociationDocument
        {
            AssociationId = associationId, Title = request.Title.Trim(), TitleEn = Normalize(request.TitleEn),
            Description = Normalize(request.Description), DescriptionEn = Normalize(request.DescriptionEn), FileName = Path.GetFileName(file.FileName),
            Url = saved.relativeUrl, ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length, Visibility = string.Equals(request.Visibility, "Managers", StringComparison.OrdinalIgnoreCase) ? "Managers" : "Members", UploadedByUserId = userId
        };
        context.AssociationDocuments.Add(item);
        await context.SaveChangesAsync();
        return ApiResponse<AssociationDocumentDto>.SuccessResponse(MapDocument(item));
    }

    public async Task<ApiResponse> DeleteDocumentAsync(Guid userId, Guid associationId, Guid documentId)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(DocumentsManage)) return ApiResponse.CreateError("Document management access denied");
        var item = await context.AssociationDocuments.FirstOrDefaultAsync(candidate => candidate.Id == documentId && candidate.AssociationId == associationId);
        if (item is null) return ApiResponse.CreateError("Document not found");
        context.AssociationDocuments.Remove(item);
        await context.SaveChangesAsync();
        await fileStorage.DeleteAsync(item.Url);
        return ApiResponse.CreateSuccess("Document deleted");
    }

    public async Task<ApiResponse<AssociationCalendarItemDto>> AddCalendarItemAsync(Guid userId, Guid associationId, CreateAssociationCalendarItemRequest request)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(CalendarManage)) return ApiResponse<AssociationCalendarItemDto>.ErrorResponse("Calendar management access denied");
        if (request.EndsAtUtc.HasValue && request.EndsAtUtc <= request.StartsAtUtc) return ApiResponse<AssociationCalendarItemDto>.ErrorResponse("End date must be after start date");
        var item = MapCalendarRequest(new AssociationCalendarItem { AssociationId = associationId, CreatedByUserId = userId }, request);
        context.AssociationCalendarItems.Add(item);
        await context.SaveChangesAsync();
        return ApiResponse<AssociationCalendarItemDto>.SuccessResponse(MapCalendar(item));
    }

    public async Task<ApiResponse<AssociationCalendarItemDto>> UpdateCalendarItemAsync(Guid userId, Guid associationId, Guid itemId, CreateAssociationCalendarItemRequest request)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(CalendarManage)) return ApiResponse<AssociationCalendarItemDto>.ErrorResponse("Calendar management access denied");
        if (request.EndsAtUtc.HasValue && request.EndsAtUtc <= request.StartsAtUtc) return ApiResponse<AssociationCalendarItemDto>.ErrorResponse("End date must be after start date");
        var item = await context.AssociationCalendarItems.FirstOrDefaultAsync(candidate => candidate.Id == itemId && candidate.AssociationId == associationId);
        if (item is null) return ApiResponse<AssociationCalendarItemDto>.ErrorResponse("Calendar item not found");
        MapCalendarRequest(item, request);
        item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return ApiResponse<AssociationCalendarItemDto>.SuccessResponse(MapCalendar(item));
    }

    public async Task<ApiResponse> DeleteCalendarItemAsync(Guid userId, Guid associationId, Guid itemId)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(CalendarManage)) return ApiResponse.CreateError("Calendar management access denied");
        var item = await context.AssociationCalendarItems.FirstOrDefaultAsync(candidate => candidate.Id == itemId && candidate.AssociationId == associationId);
        if (item is null) return ApiResponse.CreateError("Calendar item not found");
        context.AssociationCalendarItems.Remove(item);
        await context.SaveChangesAsync();
        return ApiResponse.CreateSuccess("Calendar item deleted");
    }

    public async Task<ApiResponse<ServiceCaseDto>> AddServiceCaseMessageAsync(Guid userId, Guid associationId, Guid caseId, AddServiceCaseMessageRequest request)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(ServiceCasesManage)) return ApiResponse<ServiceCaseDto>.ErrorResponse("Service request access denied");
        var item = await ServiceCases(true).FirstOrDefaultAsync(candidate => candidate.Id == caseId && candidate.AssignedAssociationId == associationId);
        if (item is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("Service request not found");
        if (item.Status == "Closed") return ApiResponse<ServiceCaseDto>.ErrorResponse("This service request is closed");
        context.ServiceCaseMessages.Add(new ServiceCaseMessage { ServiceCaseId = caseId, AuthorUserId = userId, Body = request.Body.Trim(), IsInternal = request.IsInternal });
        item.Status = request.IsInternal ? item.Status : "AwaitingMember";
        item.LastResponseAt = item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return ApiResponse<ServiceCaseDto>.SuccessResponse(MapServiceCase(item, true));
    }

    public async Task<ApiResponse<ServiceCaseDto>> UpdateServiceCaseAsync(Guid userId, Guid associationId, Guid caseId, UpdateAssociationServiceCaseRequest request)
    {
        var access = await AccessAsync(userId, associationId);
        if (access is null || !access.Permissions.Contains(ServiceCasesManage)) return ApiResponse<ServiceCaseDto>.ErrorResponse("Service request access denied");
        var status = CaseStatuses.FirstOrDefault(item => item.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        if (status is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("Unsupported service request status");
        var item = await ServiceCases(true).FirstOrDefaultAsync(candidate => candidate.Id == caseId && candidate.AssignedAssociationId == associationId);
        if (item is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("Service request not found");
        item.Status = status; item.ResolvedAt = status == "Resolved" ? DateTime.UtcNow : null; item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return ApiResponse<ServiceCaseDto>.SuccessResponse(MapServiceCase(item, true));
    }

    public async Task<ApiResponse<List<AssociationClaimDto>>> GetForAdminAsync(string? status)
    {
        var query = Claims();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
        var items = await query.OrderBy(item => item.Status == "Pending" ? 0 : 1).ThenByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<AssociationClaimDto>>.SuccessResponse(items.Select(MapClaim).ToList());
    }

    public async Task<ApiResponse<AssociationClaimDto>> ReviewAsync(Guid id, ReviewAssociationClaimRequest request)
    {
        var status = request.Status.Trim();
        if (status is not ("Approved" or "Rejected")) return ApiResponse<AssociationClaimDto>.ErrorResponse("Status must be Approved or Rejected");
        var item = await Claims(true).FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (item is null) return ApiResponse<AssociationClaimDto>.ErrorResponse("Association claim not found");
        item.Status = status; item.AdminNotes = Normalize(request.AdminNotes); item.ReviewedAt = item.UpdatedAt = DateTime.UtcNow;
        if (status == "Approved")
        {
            if (item.Association!.OwnerMemberId is not null && item.Association.OwnerMemberId != item.MemberId) return ApiResponse<AssociationClaimDto>.ErrorResponse("This organization already has an approved manager");
            item.Association.OwnerMemberId = item.MemberId;
            var membership = await context.AssociationMembers.FirstOrDefaultAsync(candidate => candidate.AssociationId == item.AssociationId && candidate.MemberId == item.MemberId);
            if (membership is null) context.AssociationMembers.Add(new AssociationMember { AssociationId = item.AssociationId, MemberId = item.MemberId, Role = "Owner", Permissions = string.Join(',', AllPermissions) });
            else { membership.Role = "Owner"; membership.Status = "Active"; membership.Permissions = string.Join(',', AllPermissions); membership.UpdatedAt = DateTime.UtcNow; }
            foreach (var competing in await context.AssociationClaimRequests.Where(candidate => candidate.AssociationId == item.AssociationId && candidate.Id != item.Id && candidate.Status == "Pending").ToListAsync())
            { competing.Status = "Rejected"; competing.AdminNotes = "Another manager was approved."; competing.ReviewedAt = competing.UpdatedAt = DateTime.UtcNow; }
        }
        await context.SaveChangesAsync();
        return ApiResponse<AssociationClaimDto>.SuccessResponse(MapClaim(item));
    }

    public async Task<ApiResponse<AssociationWorkspaceDto>> GetWorkspaceForAdminAsync(Guid associationId)
    {
        var association = await context.Associations.FirstOrDefaultAsync(item => item.Id == associationId);
        if (association is null) return ApiResponse<AssociationWorkspaceDto>.ErrorResponse("Organization not found");
        var access = new AccessContext(association, Guid.Empty, "Administrator", null, AllPermissions.ToHashSet(StringComparer.OrdinalIgnoreCase));
        return ApiResponse<AssociationWorkspaceDto>.SuccessResponse(await BuildWorkspaceAsync(access, true));
    }

    public Task<ApiResponse<AssociationJoinRequestDto>> ReviewJoinForAdminAsync(Guid userId, Guid associationId, Guid requestId, ReviewAssociationJoinRequest request) =>
        ReviewJoinCoreAsync(userId, associationId, requestId, request);

    public async Task<ApiResponse<AssociationMemberDto>> UpsertMemberForAdminAsync(Guid associationId, UpsertAssociationMemberRequest request)
    {
        if (!await context.Associations.AnyAsync(item => item.Id == associationId)) return ApiResponse<AssociationMemberDto>.ErrorResponse("Organization not found");
        if (!await context.Members.AnyAsync(item => item.Id == request.MemberId)) return ApiResponse<AssociationMemberDto>.ErrorResponse("Member not found");
        var item = await context.AssociationMembers.Include(candidate => candidate.Member).FirstOrDefaultAsync(candidate => candidate.AssociationId == associationId && candidate.MemberId == request.MemberId);
        if (item is null) { item = new AssociationMember { AssociationId = associationId, MemberId = request.MemberId }; context.AssociationMembers.Add(item); }
        ApplyMember(item, request.Role, request.Title, request.Permissions, request.Status);
        await context.SaveChangesAsync();
        item.Member ??= await context.Members.FindAsync(request.MemberId);
        return ApiResponse<AssociationMemberDto>.SuccessResponse(MapMember(item));
    }

    public Task<ApiResponse> RemoveMemberForAdminAsync(Guid associationId, Guid associationMemberId) => RemoveMemberCoreAsync(associationId, associationMemberId);

    private async Task<ApiResponse<AssociationJoinRequestDto>> ReviewJoinCoreAsync(Guid userId, Guid associationId, Guid requestId, ReviewAssociationJoinRequest request)
    {
        var status = JoinStatuses.FirstOrDefault(item => item.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        if (status is null) return ApiResponse<AssociationJoinRequestDto>.ErrorResponse("Status must be Approved or Rejected");
        var item = await JoinRequests(true).FirstOrDefaultAsync(candidate => candidate.Id == requestId && candidate.AssociationId == associationId);
        if (item is null) return ApiResponse<AssociationJoinRequestDto>.ErrorResponse("Membership request not found");
        item.Status = status; item.ReviewNotes = Normalize(request.ReviewNotes); item.ReviewedByUserId = userId; item.ReviewedAt = item.UpdatedAt = DateTime.UtcNow;
        if (status == "Approved")
        {
            var membership = await context.AssociationMembers.FirstOrDefaultAsync(candidate => candidate.AssociationId == associationId && candidate.MemberId == item.MemberId);
            if (membership is null) { membership = new AssociationMember { AssociationId = associationId, MemberId = item.MemberId }; context.AssociationMembers.Add(membership); }
            ApplyMember(membership, request.Role, request.Title, request.Permissions, "Active");
        }
        await context.SaveChangesAsync();
        var applicantUserId = await context.Users.AsNoTracking()
            .Where(candidate => candidate.MemberId == item.MemberId && candidate.IsActive)
            .Select(candidate => (Guid?)candidate.Id).FirstOrDefaultAsync();
        if (applicantUserId.HasValue)
        {
            var associationName = await context.Associations.AsNoTracking()
                .Where(candidate => candidate.Id == associationId).Select(candidate => candidate.Name).FirstAsync();
            var title = status == "Approved" ? "Adhésion approuvée" : "Demande d’adhésion mise à jour";
            var message = status == "Approved"
                ? $"Bienvenue dans l’espace privé de {associationName}."
                : $"Votre demande pour {associationName} n’a pas été retenue.";
            await notifications.CreateForUserAsync(applicantUserId.Value, "association-membership", title, message, associationId, "/espace-membre?section=associations");
        }
        return ApiResponse<AssociationJoinRequestDto>.SuccessResponse(MapJoin(item));
    }

    private async Task<ApiResponse<AssociationMemberDto>> UpdateMemberCoreAsync(Guid associationId, Guid associationMemberId, UpdateAssociationMemberRequest request)
    {
        var item = await context.AssociationMembers.Include(candidate => candidate.Member).FirstOrDefaultAsync(candidate => candidate.Id == associationMemberId && candidate.AssociationId == associationId);
        if (item is null) return ApiResponse<AssociationMemberDto>.ErrorResponse("Organization member not found");
        if (item.Role == "Owner") return ApiResponse<AssociationMemberDto>.ErrorResponse("The owner role cannot be changed here");
        ApplyMember(item, request.Role, request.Title, request.Permissions, request.Status);
        await context.SaveChangesAsync();
        return ApiResponse<AssociationMemberDto>.SuccessResponse(MapMember(item));
    }

    private async Task<ApiResponse> RemoveMemberCoreAsync(Guid associationId, Guid associationMemberId)
    {
        var item = await context.AssociationMembers.FirstOrDefaultAsync(candidate => candidate.Id == associationMemberId && candidate.AssociationId == associationId);
        if (item is null) return ApiResponse.CreateError("Organization member not found");
        if (item.Role == "Owner") return ApiResponse.CreateError("The owner cannot be removed");
        context.AssociationMembers.Remove(item);
        await context.SaveChangesAsync();
        return ApiResponse.CreateSuccess("Organization member removed");
    }

    private async Task<AssociationWorkspaceDto> BuildWorkspaceAsync(AccessContext access, bool admin = false)
    {
        if (access.Association.OwnerMemberId is Guid ownerMemberId &&
            !await context.AssociationMembers.AnyAsync(item => item.AssociationId == access.Association.Id && item.MemberId == ownerMemberId))
        {
            var owner = new AssociationMember { AssociationId = access.Association.Id, MemberId = ownerMemberId, Role = "Owner", Title = access.Association.President, Status = "Active" };
            ApplyMember(owner, "Owner", access.Association.President, AllPermissions, "Active");
            context.AssociationMembers.Add(owner);
            await context.SaveChangesAsync();
        }
        var members = await context.AssociationMembers.AsNoTracking().Include(item => item.Member).Where(item => item.AssociationId == access.Association.Id).OrderBy(item => item.Role == "Owner" ? 0 : 1).ThenBy(item => item.Member!.LastName).ToListAsync();
        var requests = admin || access.Permissions.Contains(MembersManage) ? await JoinRequests().Where(item => item.AssociationId == access.Association.Id && item.Status == "Pending").OrderBy(item => item.CreatedAt).ToListAsync() : [];
        var documentsQuery = context.AssociationDocuments.AsNoTracking().Where(item => item.AssociationId == access.Association.Id);
        if (!admin && !access.Permissions.Contains(DocumentsManage)) documentsQuery = documentsQuery.Where(item => item.Visibility == "Members");
        var documents = await documentsQuery.OrderByDescending(item => item.CreatedAt).ToListAsync();
        var calendar = await context.AssociationCalendarItems.AsNoTracking().Where(item => item.AssociationId == access.Association.Id).OrderBy(item => item.StartsAtUtc).ToListAsync();
        var cases = admin || access.Permissions.Contains(ServiceCasesManage) ? await ServiceCases().Where(item => item.AssignedAssociationId == access.Association.Id).OrderByDescending(item => item.UpdatedAt).ToListAsync() : [];
        return new AssociationWorkspaceDto(MapAssociation(access.Association), new AssociationAccessDto(access.Role, access.Title, access.Permissions.OrderBy(item => item).ToList()),
            members.Select(MapMember).ToList(), requests.Select(MapJoin).ToList(), documents.Select(MapDocument).ToList(), calendar.Select(MapCalendar).ToList(), cases.Select(item => MapServiceCase(item, true)).ToList());
    }

    private async Task<AccessContext?> AccessAsync(Guid userId, Guid associationId)
    {
        var memberId = await MemberIdAsync(userId);
        if (memberId is null) return null;
        var association = await context.Associations.FirstOrDefaultAsync(item => item.Id == associationId && item.IsActive);
        if (association is null) return null;
        if (association.OwnerMemberId == memberId) return new AccessContext(association, memberId.Value, "Owner", association.President, AllPermissions.ToHashSet(StringComparer.OrdinalIgnoreCase));
        var membership = await context.AssociationMembers.AsNoTracking().FirstOrDefaultAsync(item => item.AssociationId == associationId && item.MemberId == memberId && item.Status == "Active");
        if (membership is null) return null;
        var permissions = ParsePermissions(membership.Permissions); permissions.Add(WorkspaceView);
        return new AccessContext(association, memberId.Value, membership.Role, membership.Title, permissions);
    }

    private IQueryable<AssociationClaimRequest> Claims(bool tracking = false) { var query = context.AssociationClaimRequests.Include(item => item.Association).Include(item => item.Member); return tracking ? query : query.AsNoTracking(); }
    private IQueryable<AssociationJoinRequest> JoinRequests(bool tracking = false) { var query = context.AssociationJoinRequests.Include(item => item.Member); return tracking ? query : query.AsNoTracking(); }
    private IQueryable<ServiceCase> ServiceCases(bool tracking = false) { var query = context.ServiceCases.Include(item => item.Member).Include(item => item.AssignedToUser).Include(item => item.AssignedAssociation).Include(item => item.Messages).ThenInclude(item => item.AuthorUser).Include(item => item.Attachments).AsSplitQuery(); return tracking ? query : query.AsNoTracking(); }
    private Task<Guid?> MemberIdAsync(Guid userId) => context.Users.AsNoTracking().Where(item => item.Id == userId && item.IsActive).Select(item => item.MemberId).SingleOrDefaultAsync();
    private static AssociationClaimDto MapClaim(AssociationClaimRequest item) => new(item.Id, item.AssociationId, item.Association?.Name ?? "", item.MemberId, $"{item.Member?.FirstName} {item.Member?.LastName}".Trim(), item.Member?.Email ?? "", item.Message, item.Status, item.AdminNotes, item.CreatedAt, item.UpdatedAt, item.ReviewedAt);
    private static AssociationDto MapAssociation(Association item) => new(item.Id, item.Name, item.Description, item.Province, item.City, item.Contact, item.Phone, item.President, item.MemberCount, item.FoundedYear, item.ImageUrl, item.Website, item.Domains, item.IsActive, item.CreatedAt, item.UpdatedAt, item.NameEn, item.DescriptionEn, item.DomainsEn, item.OrganizationType);
    private static AssociationMemberDto MapMember(AssociationMember item) => new(item.Id, item.MemberId, $"{item.Member?.FirstName} {item.Member?.LastName}".Trim(), item.Member?.Email ?? "", item.Role, item.Title, ParsePermissions(item.Permissions).OrderBy(value => value).ToList(), item.Status, item.JoinedAt, item.UpdatedAt);
    private static AssociationJoinRequestDto MapJoin(AssociationJoinRequest item) => new(item.Id, item.AssociationId, item.MemberId, $"{item.Member?.FirstName} {item.Member?.LastName}".Trim(), item.Member?.Email ?? "", item.Message, item.Status, item.ReviewNotes, item.CreatedAt, item.UpdatedAt, item.ReviewedAt);
    private static AssociationDocumentDto MapDocument(AssociationDocument item) => new(item.Id, item.Title, item.TitleEn, item.Description, item.DescriptionEn, item.FileName, item.Url, item.ContentType, item.SizeBytes, item.Visibility, item.CreatedAt);
    private static AssociationCalendarItemDto MapCalendar(AssociationCalendarItem item) => new(item.Id, item.Title, item.TitleEn, item.Description, item.DescriptionEn, item.Location, item.LocationEn, item.StartsAtUtc, item.EndsAtUtc, item.CreatedAt, item.UpdatedAt);
    private static ServiceCaseDto MapServiceCase(ServiceCase item, bool includeInternal) => new(item.Id, item.TicketNumber, item.MemberId, $"{item.Member?.FirstName} {item.Member?.LastName}".Trim(), item.Member?.Email ?? "", item.Category, item.Subject, item.Description, item.Status, item.Priority, item.AssignedToUserId, item.AssignedToUser is null ? null : $"{item.AssignedToUser.FirstName} {item.AssignedToUser.LastName}".Trim(), includeInternal ? item.InternalNotes : null, item.AssignedAssociationId, item.AssignedAssociation?.Name, item.CreatedAt, item.UpdatedAt, item.LastResponseAt, item.ResolvedAt, item.Messages.Where(message => includeInternal || !message.IsInternal).OrderBy(message => message.CreatedAt).Select(message => new ServiceCaseMessageDto(message.Id, message.AuthorUserId, $"{message.AuthorUser?.FirstName} {message.AuthorUser?.LastName}".Trim(), message.Body, message.IsInternal, message.CreatedAt)).ToList(), item.Attachments.Where(attachment => includeInternal || !attachment.IsInternal).OrderBy(attachment => attachment.CreatedAt).Select(attachment => new ServiceCaseAttachmentDto(attachment.Id, attachment.FileName, attachment.Url, attachment.ContentType, attachment.SizeBytes, attachment.IsInternal, attachment.CreatedAt)).ToList());

    private static AssociationCalendarItem MapCalendarRequest(AssociationCalendarItem item, CreateAssociationCalendarItemRequest request) { item.Title = request.Title.Trim(); item.TitleEn = Normalize(request.TitleEn); item.Description = Normalize(request.Description); item.DescriptionEn = Normalize(request.DescriptionEn); item.Location = Normalize(request.Location); item.LocationEn = Normalize(request.LocationEn); item.StartsAtUtc = request.StartsAtUtc; item.EndsAtUtc = request.EndsAtUtc; return item; }
    private static void ApplyMember(AssociationMember item, string role, string? title, IReadOnlyList<string>? permissions, string status) { item.Role = Roles.FirstOrDefault(value => value.Equals(role, StringComparison.OrdinalIgnoreCase)) ?? "Member"; item.Title = Normalize(title); item.Status = MemberStatuses.FirstOrDefault(value => value.Equals(status, StringComparison.OrdinalIgnoreCase)) ?? "Active"; var effective = item.Role == "Owner" ? AllPermissions : permissions ?? DefaultPermissions(item.Role); item.Permissions = string.Join(',', effective.Where(AllPermissions.Contains).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value)); item.UpdatedAt = DateTime.UtcNow; }
    private static IReadOnlyList<string> DefaultPermissions(string role) => role switch { "Manager" => AllPermissions, "Editor" => [WorkspaceView, ProfileManage, DocumentsManage, CalendarManage], _ => [WorkspaceView] };
    private static HashSet<string> ParsePermissions(string? value) => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(AllPermissions.Contains).ToHashSet(StringComparer.OrdinalIgnoreCase);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private sealed record AccessContext(Association Association, Guid MemberId, string Role, string? Title, HashSet<string> Permissions);
}
