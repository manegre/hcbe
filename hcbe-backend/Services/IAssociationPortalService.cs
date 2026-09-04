using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IAssociationPortalService
{
    Task<ApiResponse<List<AssociationClaimDto>>> GetMineAsync(Guid userId);
    Task<ApiResponse<AssociationClaimDto>> ClaimAsync(Guid userId, Guid associationId, CreateAssociationClaimRequest request);
    Task<ApiResponse<List<AssociationDto>>> GetManagedAsync(Guid userId);
    Task<ApiResponse<AssociationDto>> UpdateManagedAsync(Guid userId, Guid associationId, UpdateAssociationRequest request);
    Task<ApiResponse<List<AssociationJoinRequestDto>>> GetMyJoinRequestsAsync(Guid userId);
    Task<ApiResponse<AssociationJoinRequestDto>> JoinAsync(Guid userId, Guid associationId, CreateAssociationJoinRequest request);
    Task<ApiResponse<AssociationWorkspaceDto>> GetWorkspaceAsync(Guid userId, Guid associationId);
    Task<ApiResponse<AssociationJoinRequestDto>> ReviewJoinAsync(Guid userId, Guid associationId, Guid requestId, ReviewAssociationJoinRequest request);
    Task<ApiResponse<AssociationMemberDto>> UpdateMemberAsync(Guid userId, Guid associationId, Guid associationMemberId, UpdateAssociationMemberRequest request);
    Task<ApiResponse> RemoveMemberAsync(Guid userId, Guid associationId, Guid associationMemberId);
    Task<ApiResponse<AssociationDocumentDto>> AddDocumentAsync(Guid userId, Guid associationId, IFormFile file, CreateAssociationDocumentRequest request);
    Task<ApiResponse> DeleteDocumentAsync(Guid userId, Guid associationId, Guid documentId);
    Task<ApiResponse<AssociationCalendarItemDto>> AddCalendarItemAsync(Guid userId, Guid associationId, CreateAssociationCalendarItemRequest request);
    Task<ApiResponse<AssociationCalendarItemDto>> UpdateCalendarItemAsync(Guid userId, Guid associationId, Guid itemId, CreateAssociationCalendarItemRequest request);
    Task<ApiResponse> DeleteCalendarItemAsync(Guid userId, Guid associationId, Guid itemId);
    Task<ApiResponse<ServiceCaseDto>> AddServiceCaseMessageAsync(Guid userId, Guid associationId, Guid caseId, AddServiceCaseMessageRequest request);
    Task<ApiResponse<ServiceCaseDto>> UpdateServiceCaseAsync(Guid userId, Guid associationId, Guid caseId, UpdateAssociationServiceCaseRequest request);
    Task<ApiResponse<List<AssociationClaimDto>>> GetForAdminAsync(string? status);
    Task<ApiResponse<AssociationClaimDto>> ReviewAsync(Guid id, ReviewAssociationClaimRequest request);
    Task<ApiResponse<AssociationWorkspaceDto>> GetWorkspaceForAdminAsync(Guid associationId);
    Task<ApiResponse<AssociationJoinRequestDto>> ReviewJoinForAdminAsync(Guid userId, Guid associationId, Guid requestId, ReviewAssociationJoinRequest request);
    Task<ApiResponse<AssociationMemberDto>> UpsertMemberForAdminAsync(Guid associationId, UpsertAssociationMemberRequest request);
    Task<ApiResponse> RemoveMemberForAdminAsync(Guid associationId, Guid associationMemberId);
}
