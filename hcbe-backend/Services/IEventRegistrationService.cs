using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IEventRegistrationService
{
    Task<ApiResponse<EventRegistrationDto>> RegisterAsync(Guid userId, Guid eventId, CreateEventRegistrationRequest request);
    Task<ApiResponse<EventRegistrationDto>> GetMineForEventAsync(Guid userId, Guid eventId);
    Task<ApiResponse<List<EventRegistrationDto>>> GetMineAsync(Guid userId);
    Task<ApiResponse<EventRegistrationDto>> CancelAsync(Guid userId, Guid eventId);
    Task<ApiResponse<List<EventRegistrationDto>>> GetForAdminAsync(Guid eventId, string? status, string? search);
    Task<ApiResponse<EventRegistrationDto>> UpdateForAdminAsync(Guid eventId, Guid registrationId, UpdateEventRegistrationRequest request);
    Task<(byte[]? Content, string? FileName)> BuildCalendarAsync(Guid eventId);
}
