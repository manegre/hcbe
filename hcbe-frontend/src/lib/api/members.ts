import { apiClient } from './client';
import type { ApiResponse, CreateMemberRequest, MemberDto, PagedResult, UpdateMemberRequest } from './types';

export const membersApi = {
  getAllMembers: (): Promise<ApiResponse<MemberDto[]>> =>
    apiClient.get<MemberDto[]>('/api/members/admin'),

  searchMembers: (params: { page: number; pageSize?: number; search?: string; sort?: string }): Promise<ApiResponse<PagedResult<MemberDto>>> => {
    const searchParams = new URLSearchParams({ page: String(params.page), pageSize: String(params.pageSize ?? 15) });
    if (params.search) searchParams.set('search', params.search);
    if (params.sort) searchParams.set('sort', params.sort);
    return apiClient.get<PagedResult<MemberDto>>(`/api/members/admin/paged?${searchParams}`);
  },

  getMemberById: (id: string): Promise<ApiResponse<MemberDto>> =>
    apiClient.get<MemberDto>(`/api/members/${id}`),

  createMember: (data: CreateMemberRequest): Promise<ApiResponse<MemberDto>> =>
    apiClient.post<MemberDto>('/api/members', data),

  updateMember: (id: string, data: UpdateMemberRequest): Promise<ApiResponse<MemberDto>> =>
    apiClient.put<MemberDto>(`/api/members/${id}`, data),

  deleteMember: (id: string): Promise<ApiResponse<boolean>> =>
    apiClient.delete<boolean>(`/api/members/${id}`),

  updateAdminStatus: (id: string, isAdmin: boolean): Promise<ApiResponse<MemberDto>> =>
    apiClient.put<MemberDto>(`/api/members/${id}/admin?isAdmin=${isAdmin}`),
};
