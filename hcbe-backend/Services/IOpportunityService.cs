using HcbeApi.Helpers;
using HcbeApi.Models;
namespace HcbeApi.Services;
public interface IOpportunityService
{
    Task<ApiResponse<List<OpportunityDto>>> GetPublishedAsync(string? type);
    Task<ApiResponse<List<OpportunityMatchDto>>> GetMatchedAsync(Guid userId, string? type);
    Task<ApiResponse<List<OpportunityDto>>> GetForAdminAsync();
    Task<ApiResponse<OpportunityDto>> CreateAsync(Guid userId, UpsertOpportunityRequest request);
    Task<ApiResponse<OpportunityDto>> UpdateAsync(Guid id, UpsertOpportunityRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
    Task<ApiResponse<OpportunityApplicationDto>> ApplyAsync(Guid userId, Guid id, CreateOpportunityApplicationRequest request);
    Task<ApiResponse<OpportunityApplicationDocumentDto>> AddApplicationDocumentAsync(Guid userId, Guid applicationId, IFormFile file);
    Task<ApiResponse<OpportunityDocumentContent>> GetApplicationDocumentAsync(Guid userId, Guid applicationId, Guid documentId, bool isAdmin);
    Task<ApiResponse> DeleteApplicationDocumentAsync(Guid userId, Guid applicationId, Guid documentId);
    Task<ApiResponse<VolunteerTimeEntryDto>> AddVolunteerTimeAsync(Guid userId, Guid applicationId, CreateVolunteerTimeEntryRequest request);
    Task<ApiResponse<OpportunityCertificateDto>> IssueCertificateAsync(Guid userId, Guid applicationId, IssueOpportunityCertificateRequest request);
    Task<ApiResponse<byte[]>> GetCertificatePdfAsync(Guid userId, Guid applicationId, bool isAdmin);
    Task<ApiResponse<List<OpportunityApplicationDto>>> GetMineAsync(Guid userId);
    Task<ApiResponse<List<OpportunityApplicationDto>>> GetApplicationsAsync(Guid? opportunityId);
    Task<ApiResponse<OpportunityApplicationDto>> ReviewApplicationAsync(Guid id, ReviewOpportunityApplicationRequest request);
    Task<ApiResponse<VolunteerTimeEntryDto>> ReviewVolunteerTimeAsync(Guid userId, Guid id, ReviewVolunteerTimeEntryRequest request);
}
public sealed record OpportunityDocumentContent(byte[] Bytes, string ContentType, string FileName);
