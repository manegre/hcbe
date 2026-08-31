using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public interface IMessagingService
{
    Task<ApiResponse<List<MessagingContactDto>>> GetEligibleContactsAsync(Guid userId);
    Task<ApiResponse<List<ConversationDto>>> GetConversationsAsync(Guid userId);
    Task<ApiResponse<ConversationDto>> StartConversationAsync(Guid userId, StartConversationRequest request);
    Task<ApiResponse<List<PrivateMessageDto>>> GetMessagesAsync(Guid userId, Guid conversationId);
    Task<ApiResponse<PrivateMessageDto>> SendMessageAsync(Guid userId, Guid conversationId, SendPrivateMessageRequest request);
    Task<ApiResponse> MarkConversationReadAsync(Guid userId, Guid conversationId);
    Task<ApiResponse<ConversationReportDto>> ReportConversationAsync(Guid userId, Guid conversationId, ReportConversationRequest request);
    Task<ApiResponse<List<ConversationReportDto>>> GetReportsForAdminAsync(string? status);
    Task<ApiResponse<ConversationReportDto>> ResolveReportAsync(Guid id, ResolveConversationReportRequest request);
}
