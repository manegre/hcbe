import { apiClient } from './client';
import type {
  ApiResponse,
  CreateMembershipApplicationRequest,
  MemberDto,
  MembershipApplicationDto,
  MembershipApplicationStatus,
  PagedResult,
} from './types';

export const membershipApplicationsApi = {
  submit: (data: CreateMembershipApplicationRequest): Promise<ApiResponse<MembershipApplicationDto>> =>
    apiClient.post<MembershipApplicationDto>('/api/membership-applications', data, false),

  getAll: (status?: MembershipApplicationStatus): Promise<ApiResponse<MembershipApplicationDto[]>> => {
    const query = status ? `?status=${status}` : '';
    return apiClient.get<MembershipApplicationDto[]>(`/api/membership-applications/admin${query}`);
  },

  search: (params: { page: number; pageSize?: number; search?: string; sort?: string; status?: MembershipApplicationStatus }): Promise<ApiResponse<PagedResult<MembershipApplicationDto>>> => {
    const query = new URLSearchParams({ page: String(params.page), pageSize: String(params.pageSize ?? 15) });
    if (params.search) query.set('search', params.search);
    if (params.sort) query.set('sort', params.sort);
    if (params.status) query.set('status', params.status);
    return apiClient.get<PagedResult<MembershipApplicationDto>>(`/api/membership-applications/admin/paged?${query}`);
  },

  getById: (id: string): Promise<ApiResponse<MembershipApplicationDto>> =>
    apiClient.get<MembershipApplicationDto>(`/api/membership-applications/admin/${id}`),

  approve: (id: string): Promise<ApiResponse<MemberDto>> =>
    apiClient.post<MemberDto>(`/api/membership-applications/${id}/approve`, {}),

  reject: (id: string, reason?: string): Promise<ApiResponse<MembershipApplicationDto>> =>
    apiClient.post<MembershipApplicationDto>(
      `/api/membership-applications/${id}/reject`,
      reason ? { reason } : {},
    ),

  delete: (id: string): Promise<ApiResponse<boolean>> =>
    apiClient.delete<boolean>(`/api/membership-applications/${id}`),
};
