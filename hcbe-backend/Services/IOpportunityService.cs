using HcbeApi.Helpers;
using HcbeApi.Models;
namespace HcbeApi.Services;
public interface IOpportunityService
{
    Task<ApiResponse<List<OpportunityDto>>> GetPublishedAsync(string? type);
    Task<ApiResponse<List<OpportunityDto>>> GetForAdminAsync();
    Task<ApiResponse<OpportunityDto>> CreateAsync(Guid userId, UpsertOpportunityRequest request);
    Task<ApiResponse<OpportunityDto>> UpdateAsync(Guid id, UpsertOpportunityRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
    Task<ApiResponse<OpportunityApplicationDto>> ApplyAsync(Guid userId, Guid id, CreateOpportunityApplicationRequest request);
    Task<ApiResponse<List<OpportunityApplicationDto>>> GetMineAsync(Guid userId);
    Task<ApiResponse<List<OpportunityApplicationDto>>> GetApplicationsAsync(Guid? opportunityId);
    Task<ApiResponse<OpportunityApplicationDto>> ReviewApplicationAsync(Guid id, ReviewOpportunityApplicationRequest request);
}
