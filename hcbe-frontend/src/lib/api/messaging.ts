import { apiClient } from './client';
import type { ApiResponse, ConversationDto, ConversationReportDto, MessagingContactDto, PrivateMessageDto } from './types';

export const messagingApi = {
  getContacts: (): Promise<ApiResponse<MessagingContactDto[]>> => apiClient.get('/api/community/messages/contacts'),
  getConversations: (): Promise<ApiResponse<ConversationDto[]>> => apiClient.get('/api/community/messages/conversations'),
  startConversation: (memberId: string): Promise<ApiResponse<ConversationDto>> => apiClient.post('/api/community/messages/conversations', { memberId }),
  getMessages: (conversationId: string): Promise<ApiResponse<PrivateMessageDto[]>> => apiClient.get(`/api/community/messages/conversations/${conversationId}`),
  sendMessage: (conversationId: string, body: string): Promise<ApiResponse<PrivateMessageDto>> => apiClient.post(`/api/community/messages/conversations/${conversationId}`, { body }),
  markRead: (conversationId: string): Promise<ApiResponse<unknown>> => apiClient.post(`/api/community/messages/conversations/${conversationId}/read`, {}),
  report: (conversationId: string, reason: string): Promise<ApiResponse<ConversationReportDto>> => apiClient.post(`/api/community/messages/conversations/${conversationId}/report`, { reason }),
  adminGetReports: (status?: string): Promise<ApiResponse<ConversationReportDto[]>> => apiClient.get(`/api/admin/message-reports${status ? `?status=${encodeURIComponent(status)}` : ''}`),
  adminResolveReport: (id: string, status: 'Resolved' | 'Dismissed', adminNotes: string, suspendConversation: boolean): Promise<ApiResponse<ConversationReportDto>> => apiClient.patch(`/api/admin/message-reports/${id}`, { status, adminNotes, suspendConversation }),
};
