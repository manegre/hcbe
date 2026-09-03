import { apiClient } from './client';
import type { ApiResponse, MemberDto, MemberOnboarding, MemberPreference, UpdateMemberAccountRequest, UpdateMemberPreferenceRequest } from './types';

export const memberAccountApi = {
  getMe: (): Promise<ApiResponse<MemberDto>> => apiClient.get<MemberDto>('/api/member-account/me'),
  updateMe: (data: UpdateMemberAccountRequest): Promise<ApiResponse<MemberDto>> =>
    apiClient.put<MemberDto>('/api/member-account/me', data),
  getOnboarding: (): Promise<ApiResponse<MemberOnboarding>> =>
    apiClient.get<MemberOnboarding>('/api/member-account/onboarding'),
  updatePreferences: (data: UpdateMemberPreferenceRequest): Promise<ApiResponse<MemberPreference>> =>
    apiClient.put<MemberPreference>('/api/member-account/preferences', data),
};
