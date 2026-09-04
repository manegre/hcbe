import { apiClient } from './client';
import type { ApiResponse, AppNotification } from './types';

export const notificationsApi = {
  getAll: (limit = 50): Promise<ApiResponse<AppNotification[]>> => apiClient.get(`/api/notifications?limit=${limit}&member=true`),
  unreadCount: (): Promise<ApiResponse<number>> => apiClient.get('/api/notifications/unread-count?member=true'),
  markRead: (id: string): Promise<ApiResponse<AppNotification>> => apiClient.put(`/api/notifications/${id}/read?member=true`),
  markAllRead: (): Promise<ApiResponse<unknown>> => apiClient.put('/api/notifications/mark-all-read?member=true'),
};
