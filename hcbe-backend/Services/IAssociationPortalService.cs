using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IAssociationPortalService
{
    Task<ApiResponse<List<AssociationClaimDto>>> GetMineAsync(Guid userId);
    Task<ApiResponse<AssociationClaimDto>> ClaimAsync(Guid userId, Guid associationId, CreateAssociationClaimRequest request);
    Task<ApiResponse<List<AssociationDto>>> GetManagedAsync(Guid userId);
    Task<ApiResponse<AssociationDto>> UpdateManagedAsync(Guid userId, Guid associationId, UpdateAssociationRequest request);
    Task<ApiResponse<List<AssociationClaimDto>>> GetForAdminAsync(string? status);
    Task<ApiResponse<AssociationClaimDto>> ReviewAsync(Guid id, ReviewAssociationClaimRequest request);
}
