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
    Task<ApiResponse<EventRegistrationDto>> CheckInByCodeAsync(Guid eventId, string confirmationCode);
    Task<(byte[]? Content, string? FileName)> BuildCalendarAsync(Guid eventId);
    Task<ApiResponse<EventAttendanceStatsDto>> GetStatsAsync(Guid eventId);
    Task<ApiResponse<EventSurveyResponseDto>> SubmitSurveyAsync(Guid userId, Guid eventId, SubmitEventSurveyRequest request);
    Task<ApiResponse<EventSurveyResponseDto>> GetMySurveyAsync(Guid userId, Guid eventId);
    Task<(byte[]? Content, string? FileName)> BuildCertificateAsync(Guid userId, Guid eventId);
    Task<ApiResponse<EventCommunicationDto>> SendCommunicationAsync(Guid userId, Guid eventId, SendEventCommunicationRequest request);
    Task<ApiResponse<List<EventCommunicationDto>>> GetCommunicationsAsync(Guid eventId);
}
