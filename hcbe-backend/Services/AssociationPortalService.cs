using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class AssociationPortalService(ApplicationDbContext context, INotificationService notifications) : IAssociationPortalService
{
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
        if (association is null || !association.IsActive) return ApiResponse<AssociationClaimDto>.ErrorResponse("Association not found");
        if (association.OwnerMemberId is not null) return ApiResponse<AssociationClaimDto>.ErrorResponse("This association already has an approved manager");
        var existing = await context.AssociationClaimRequests.Include(item => item.Association).Include(item => item.Member)
            .FirstOrDefaultAsync(item => item.AssociationId == associationId && item.MemberId == memberId && item.Status == "Pending");
        if (existing is not null) return ApiResponse<AssociationClaimDto>.SuccessResponse(MapClaim(existing));
        var item = new AssociationClaimRequest { AssociationId = associationId, MemberId = memberId.Value, Message = request.Message.Trim() };
        context.AssociationClaimRequests.Add(item);
        await context.SaveChangesAsync();
        item.Association = association;
        item.Member = await context.Members.FindAsync(memberId.Value);
        await notifications.CreateNotificationAsync("association-claim", "Nouvelle demande d’association", association.Name, item.Id, "/admin/association-requests");
        return ApiResponse<AssociationClaimDto>.SuccessResponse(MapClaim(item));
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetManagedAsync(Guid userId)
    {
        var memberId = await MemberIdAsync(userId);
        var items = memberId is null ? [] : await context.Associations.AsNoTracking().Where(item => item.OwnerMemberId == memberId).OrderBy(item => item.Name).ToListAsync();
        return ApiResponse<List<AssociationDto>>.SuccessResponse(items.Select(MapAssociation).ToList());
    }

    public async Task<ApiResponse<AssociationDto>> UpdateManagedAsync(Guid userId, Guid associationId, UpdateAssociationRequest request)
    {
        var memberId = await MemberIdAsync(userId);
        var item = memberId is null ? null : await context.Associations.FirstOrDefaultAsync(candidate => candidate.Id == associationId && candidate.OwnerMemberId == memberId);
        if (item is null) return ApiResponse<AssociationDto>.ErrorResponse("Managed association not found");
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
            if (item.Association!.OwnerMemberId is not null && item.Association.OwnerMemberId != item.MemberId)
                return ApiResponse<AssociationClaimDto>.ErrorResponse("This association already has an approved manager");
            item.Association.OwnerMemberId = item.MemberId;
            foreach (var competing in await context.AssociationClaimRequests.Where(candidate => candidate.AssociationId == item.AssociationId && candidate.Id != item.Id && candidate.Status == "Pending").ToListAsync())
            { competing.Status = "Rejected"; competing.AdminNotes = "Another manager was approved."; competing.ReviewedAt = competing.UpdatedAt = DateTime.UtcNow; }
        }
        await context.SaveChangesAsync();
        return ApiResponse<AssociationClaimDto>.SuccessResponse(MapClaim(item));
    }

    private IQueryable<AssociationClaimRequest> Claims(bool tracking = false)
    {
        var query = context.AssociationClaimRequests.Include(item => item.Association).Include(item => item.Member);
        return tracking ? query : query.AsNoTracking();
    }
    private Task<Guid?> MemberIdAsync(Guid userId) => context.Users.AsNoTracking().Where(item => item.Id == userId && item.IsActive).Select(item => item.MemberId).SingleOrDefaultAsync();
    private static AssociationClaimDto MapClaim(AssociationClaimRequest item) => new(item.Id, item.AssociationId, item.Association?.Name ?? "", item.MemberId, $"{item.Member?.FirstName} {item.Member?.LastName}".Trim(), item.Member?.Email ?? "", item.Message, item.Status, item.AdminNotes, item.CreatedAt, item.UpdatedAt, item.ReviewedAt);
    private static AssociationDto MapAssociation(Association item) => new(item.Id, item.Name, item.Description, item.Province, item.City, item.Contact, item.Phone, item.President, item.MemberCount, item.FoundedYear, item.ImageUrl, item.Website, item.Domains, item.IsActive, item.CreatedAt, item.UpdatedAt, item.NameEn, item.DescriptionEn, item.DomainsEn);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
