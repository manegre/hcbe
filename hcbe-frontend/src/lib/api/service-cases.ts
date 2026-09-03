import { apiClient } from './client';
import { getApiBaseUrl } from './base-url';
import type { ApiResponse, ServiceCase, ServiceCaseAttachment } from './types';

const upload = async (endpoint: string, file: File): Promise<ApiResponse<ServiceCaseAttachment>> => {
  const body = new FormData();
  body.append('file', file);
  const token = localStorage.getItem('hcbe_token');
  const response = await fetch(`${getApiBaseUrl()}${endpoint}`, {
    method: 'POST', credentials: 'include', body,
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  const payload = await response.json() as ApiResponse<ServiceCaseAttachment>;
  if (!response.ok) throw new Error(payload.message || `HTTP ${response.status}`);
  return payload;
};

export const serviceCasesApi = {
  mine: () => apiClient.get<ServiceCase[]>('/api/service-cases/me'),
  getMine: (id: string) => apiClient.get<ServiceCase>(`/api/service-cases/me/${id}`),
  create: (category: string, subject: string, description: string) => apiClient.post<ServiceCase>('/api/service-cases', { category, subject, description }),
  reply: (id: string, body: string) => apiClient.post<ServiceCase>(`/api/service-cases/me/${id}/messages`, { body }),
  upload: (id: string, file: File) => upload(`/api/service-cases/me/${id}/attachments`, file),
  adminList: (status?: string, category?: string, search?: string) => {
    const query = new URLSearchParams();
    if (status) query.set('status', status);
    if (category) query.set('category', category);
    if (search) query.set('search', search);
    return apiClient.get<ServiceCase[]>(`/api/admin/service-cases${query.size ? `?${query}` : ''}`);
  },
  adminGet: (id: string) => apiClient.get<ServiceCase>(`/api/admin/service-cases/${id}`),
  adminUpdate: (id: string, data: { status?: string; priority?: string; assignedToUserId?: string; clearAssignee?: boolean; internalNotes?: string }) => apiClient.patch<ServiceCase>(`/api/admin/service-cases/${id}`, data),
  adminReply: (id: string, body: string, isInternal: boolean) => apiClient.post<ServiceCase>(`/api/admin/service-cases/${id}/messages`, { body, isInternal }),
  adminUpload: (id: string, file: File, isInternal: boolean) => upload(`/api/admin/service-cases/${id}/attachments?isInternal=${isInternal}`, file),
};
