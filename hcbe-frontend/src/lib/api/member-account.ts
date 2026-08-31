import { apiClient } from './client';
import type { ApiResponse, MemberDto, UpdateMemberAccountRequest } from './types';

export const memberAccountApi = {
  getMe: (): Promise<ApiResponse<MemberDto>> => apiClient.get<MemberDto>('/api/member-account/me'),
  updateMe: (data: UpdateMemberAccountRequest): Promise<ApiResponse<MemberDto>> =>
    apiClient.put<MemberDto>('/api/member-account/me', data),
};
