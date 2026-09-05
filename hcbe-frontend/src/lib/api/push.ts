import { apiClient } from './client';
import type { ApiResponse } from './types';

export interface PushConfiguration { enabled: boolean; publicKey?: string }
export interface PushStatus { configured: boolean; deviceCount: number }
export interface PushSubscriptionPayload { endpoint: string; p256dh: string; auth: string }

export const pushApi = {
  configuration: (): Promise<ApiResponse<PushConfiguration>> => apiClient.get('/api/push/configuration'),
  status: (): Promise<ApiResponse<PushStatus>> => apiClient.get('/api/push/status'),
  subscribe: (data: PushSubscriptionPayload): Promise<ApiResponse<PushStatus>> => apiClient.post('/api/push/subscriptions', data),
  unsubscribe: (endpoint: string): Promise<ApiResponse<unknown>> => apiClient.post('/api/push/unsubscribe', { endpoint }),
  test: (language: string): Promise<ApiResponse<unknown>> => apiClient.post('/api/push/test', { language }),
};
