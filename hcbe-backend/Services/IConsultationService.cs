using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IConsultationService
{
    Task<ApiResponse<List<ConsultationDto>>> GetActiveAsync();
    Task<ApiResponse<List<ConsultationDto>>> GetAllForAdminAsync();
    Task<ApiResponse<ConsultationDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<ConsultationDto>> GetByIdForAdminAsync(Guid id);
    Task<ApiResponse<ConsultationDto>> CreateAsync(CreateConsultationRequest request);
    Task<ApiResponse<ConsultationDto>> UpdateAsync(Guid id, UpdateConsultationRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
    Task<ApiResponse<bool>> ToggleStatusAsync(Guid id);
}
