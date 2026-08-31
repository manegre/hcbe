using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IPartnerService
{
    Task<ApiResponse<List<PartnerDto>>> GetAllAsync(bool includeInactive = false);
    Task<ApiResponse<PartnerDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<PartnerDto>> CreateAsync(CreatePartnerRequest request);
    Task<ApiResponse<PartnerDto>> UpdateAsync(Guid id, UpdatePartnerRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
    Task<ApiResponse<List<PartnerDto>>> ReorderAsync(ReorderPartnersRequest request);
}
