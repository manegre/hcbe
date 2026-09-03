using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IServiceCaseService
{
    Task<ApiResponse<ServiceCaseDto>> CreateAsync(Guid userId, CreateServiceCaseRequest request);
    Task<ApiResponse<List<ServiceCaseDto>>> GetMineAsync(Guid userId);
    Task<ApiResponse<ServiceCaseDto>> GetMineByIdAsync(Guid userId, Guid id);
    Task<ApiResponse<ServiceCaseDto>> AddMemberMessageAsync(Guid userId, Guid id, AddServiceCaseMessageRequest request);
    Task<ApiResponse<ServiceCaseAttachmentDto>> AddMemberAttachmentAsync(Guid userId, Guid id, IFormFile file);
    Task<ApiResponse<List<ServiceCaseDto>>> GetForAdminAsync(string? status, string? category, string? search);
    Task<ApiResponse<ServiceCaseDto>> GetForAdminByIdAsync(Guid id);
    Task<ApiResponse<ServiceCaseDto>> UpdateForAdminAsync(Guid id, UpdateServiceCaseRequest request);
    Task<ApiResponse<ServiceCaseDto>> AddAdminMessageAsync(Guid userId, Guid id, AddServiceCaseMessageRequest request);
    Task<ApiResponse<ServiceCaseAttachmentDto>> AddAdminAttachmentAsync(Guid userId, Guid id, IFormFile file, bool isInternal);
}
