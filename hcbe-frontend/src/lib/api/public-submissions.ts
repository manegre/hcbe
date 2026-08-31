import { apiClient } from './client';
import type { ApiResponse, CreatePublicSubmissionRequest, PagedResult, PublicSubmissionDto } from './types';

export const publicSubmissionsApi = {
  submit: (data: CreatePublicSubmissionRequest): Promise<ApiResponse<PublicSubmissionDto>> =>
    apiClient.post<PublicSubmissionDto>('/api/submissions', data, false),

  getAll: (filters?: { type?: string; status?: string }): Promise<ApiResponse<PublicSubmissionDto[]>> => {
    const params = new URLSearchParams();
    if (filters?.type) params.set('type', filters.type);
    if (filters?.status) params.set('status', filters.status);
    const query = params.toString();
    return apiClient.get<PublicSubmissionDto[]>(`/api/submissions/admin${query ? `?${query}` : ''}`);
  },

  search: (filters: { page: number; pageSize?: number; search?: string; sort?: string; type?: string; status?: string }): Promise<ApiResponse<PagedResult<PublicSubmissionDto>>> => {
    const params = new URLSearchParams({ page: String(filters.page), pageSize: String(filters.pageSize ?? 15) });
    if (filters.search) params.set('search', filters.search);
    if (filters.sort) params.set('sort', filters.sort);
    if (filters.type) params.set('type', filters.type);
    if (filters.status) params.set('status', filters.status);
    return apiClient.get<PagedResult<PublicSubmissionDto>>(`/api/submissions/admin/paged?${params}`);
  },

  updateStatus: (id: string, status: PublicSubmissionDto['status']) =>
    apiClient.patch<PublicSubmissionDto>(`/api/submissions/admin/${id}/status`, { status }),

  delete: (id: string) => apiClient.delete<void>(`/api/submissions/admin/${id}`),
};
