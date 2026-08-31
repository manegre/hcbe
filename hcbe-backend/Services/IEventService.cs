using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IEventService
{
    Task<ApiResponse<List<EventDto>>> GetAllAsync();
    Task<ApiResponse<List<EventDto>>> GetAllForAdminAsync();
    Task<ApiResponse<EventDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<EventDto>> GetByIdForAdminAsync(Guid id);
    Task<ApiResponse<EventDto>> CreateAsync(CreateEventRequest request);
    Task<ApiResponse<EventDto>> UpdateAsync(Guid id, UpdateEventRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
    Task<ApiResponse<EventMediaDto>> AddPhotoAsync(Guid eventId, IFormFile file);
    Task<ApiResponse<EventMediaDto>> AddVideoAsync(Guid eventId, AddEventVideoRequest request);
    Task<ApiResponse> DeleteMediaAsync(Guid eventId, Guid mediaId);
    Task<ApiResponse<EventAttachmentDto>> AddAttachmentAsync(Guid eventId, IFormFile file);
    Task<ApiResponse> DeleteAttachmentAsync(Guid eventId, Guid attachmentId);
}
