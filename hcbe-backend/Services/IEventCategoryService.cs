using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IEventCategoryService
{
    Task<ApiResponse<List<EventCategoryDto>>> GetAllAsync(bool includeInactive = false);
    Task<ApiResponse<EventCategoryDto>> CreateAsync(CreateEventCategoryRequest request);
    Task<ApiResponse<EventCategoryDto>> UpdateAsync(Guid id, UpdateEventCategoryRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
}
