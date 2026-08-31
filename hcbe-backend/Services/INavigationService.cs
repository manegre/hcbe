using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface INavigationService
{
    Task<ApiResponse<List<NavigationItemDto>>> GetAllAsync(bool includeInactive = false);
    Task<ApiResponse<NavigationItemDto>> CreateAsync(CreateNavigationItemRequest request);
    Task<ApiResponse<NavigationItemDto>> UpdateAsync(Guid id, UpdateNavigationItemRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
}

