using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IConsultationService
{
    Task<ApiResponse<List<ConsultationDto>>> GetActiveAsync(Guid? userId = null);
    Task<ApiResponse<List<ConsultationDto>>> GetAllForAdminAsync();
    Task<ApiResponse<ConsultationDto>> GetByIdAsync(Guid id, Guid? userId = null);
    Task<ApiResponse<ConsultationDto>> GetByIdForAdminAsync(Guid id);
    Task<ApiResponse<ConsultationDto>> CreateAsync(CreateConsultationRequest request, Guid userId);
    Task<ApiResponse<ConsultationDto>> UpdateAsync(Guid id, UpdateConsultationRequest request, Guid userId);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
    Task<ApiResponse<bool>> ToggleStatusAsync(Guid id);
    Task<ApiResponse<ConsultationDto>> VoteAsync(Guid id, Guid userId, CastConsultationVoteRequest request);
    Task<ApiResponse<ConsultationCommentDto>> CommentAsync(Guid id, Guid userId, AddConsultationCommentRequest request);
    Task<ApiResponse<ConsultationDto>> PublishResultsAsync(Guid id, Guid userId, bool publish);
    Task<ApiResponse<List<ConsultationAuditEventDto>>> GetAuditAsync(Guid id);
}
