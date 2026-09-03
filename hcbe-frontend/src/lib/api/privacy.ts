import { apiClient } from './client';
import type { ApiResponse, PrivacyRequest } from './types';

export const privacyApi = {
  getDeletionRequest: (): Promise<ApiResponse<PrivacyRequest>> =>
    apiClient.get<PrivacyRequest>('/api/privacy/deletion-request'),
  requestDeletion: (): Promise<ApiResponse<PrivacyRequest>> =>
    apiClient.post<PrivacyRequest>('/api/privacy/deletion-request'),
  cancelDeletion: (): Promise<ApiResponse<null>> =>
    apiClient.delete<null>('/api/privacy/deletion-request'),
  exportData: () => apiClient.download('/api/privacy/export'),
};
