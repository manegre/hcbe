import { apiClient } from './client';
import type { ApiResponse, MemberBlock, MemberEngagementDashboard, SavedMemberItem } from './types';

export const engagementApi = {
  dashboard: (): Promise<ApiResponse<MemberEngagementDashboard>> => apiClient.get('/api/member-engagement/dashboard'),
  getSaved: (): Promise<ApiResponse<SavedMemberItem[]>> => apiClient.get('/api/member-engagement/saved'),
  save: (entityType: 'Event' | 'Opportunity', entityId: string): Promise<ApiResponse<SavedMemberItem>> =>
    apiClient.put(`/api/member-engagement/saved/${entityType}/${entityId}`),
  removeSaved: (entityType: 'Event' | 'Opportunity', entityId: string): Promise<ApiResponse<unknown>> =>
    apiClient.delete(`/api/member-engagement/saved/${entityType}/${entityId}`),
  getBlocks: (): Promise<ApiResponse<MemberBlock[]>> => apiClient.get('/api/member-engagement/blocks'),
  block: (memberId: string): Promise<ApiResponse<MemberBlock>> => apiClient.put(`/api/member-engagement/blocks/${memberId}`),
  unblock: (memberId: string): Promise<ApiResponse<unknown>> => apiClient.delete(`/api/member-engagement/blocks/${memberId}`),
};
