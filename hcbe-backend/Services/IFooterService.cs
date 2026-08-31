using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IFooterService
{
    Task<ApiResponse<List<FooterLinkDto>>> GetAllAsync(bool includeInactive = false);
    Task<ApiResponse<FooterLinkDto>> CreateAsync(CreateFooterLinkRequest request);
    Task<ApiResponse<FooterLinkDto>> UpdateAsync(Guid id, UpdateFooterLinkRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
}

