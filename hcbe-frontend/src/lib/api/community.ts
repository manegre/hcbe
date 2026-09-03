import { apiClient } from './client';
import type { ApiResponse, ConnectionRequestDto, CreateMentorshipApplicationRequest, MentorshipApplicationDto, MentorshipCheckIn, MentorshipGoal, MentorshipJourney, MentorshipMatchDto, NetworkingProfileDto, UpsertNetworkingProfileRequest } from './types';

export const communityApi = {
  getMyApplications: (): Promise<ApiResponse<MentorshipApplicationDto[]>> => apiClient.get('/api/community/mentorship/applications/me'),
  apply: (data: CreateMentorshipApplicationRequest): Promise<ApiResponse<MentorshipApplicationDto>> => apiClient.post('/api/community/mentorship/applications', data),
  withdraw: (id: string): Promise<ApiResponse<MentorshipApplicationDto>> => apiClient.post(`/api/community/mentorship/applications/${id}/withdraw`, {}),
  getMyMatches: (): Promise<ApiResponse<MentorshipMatchDto[]>> => apiClient.get('/api/community/mentorship/matches/me'),
  respondToMatch: (id: string, response: 'Accept' | 'Decline'): Promise<ApiResponse<MentorshipMatchDto>> => apiClient.post(`/api/community/mentorship/matches/${id}/respond?response=${response}`, {}),
  getJourney: (id: string): Promise<ApiResponse<MentorshipJourney>> => apiClient.get(`/api/community/mentorship/matches/${id}/journey`),
  addGoal: (id: string, title: string, dueAtUtc?: string): Promise<ApiResponse<MentorshipGoal>> => apiClient.post(`/api/community/mentorship/matches/${id}/goals`, { title, dueAtUtc }),
  updateGoal: (id: string, status: MentorshipGoal['status']): Promise<ApiResponse<MentorshipGoal>> => apiClient.put(`/api/community/mentorship/goals/${id}`, { status }),
  addCheckIn: (id: string, summary: string, rating: number, needsCommitteeSupport: boolean): Promise<ApiResponse<MentorshipCheckIn>> => apiClient.post(`/api/community/mentorship/matches/${id}/check-ins`, { summary, rating, needsCommitteeSupport }),
  getMyProfile: (): Promise<ApiResponse<NetworkingProfileDto>> => apiClient.get('/api/community/networking/profile/me'),
  saveProfile: (data: UpsertNetworkingProfileRequest): Promise<ApiResponse<NetworkingProfileDto>> => apiClient.put('/api/community/networking/profile/me', data),
  searchDirectory: (filters?: { search?: string; province?: string }): Promise<ApiResponse<NetworkingProfileDto[]>> => {
    const params = new URLSearchParams();
    if (filters?.search) params.set('search', filters.search);
    if (filters?.province) params.set('province', filters.province);
    return apiClient.get(`/api/community/networking/directory${params.toString() ? `?${params}` : ''}`);
  },
  requestConnection: (recipientMemberId: string, message: string): Promise<ApiResponse<ConnectionRequestDto>> => apiClient.post('/api/community/networking/requests', { recipientMemberId, message }),
  getMyRequests: (): Promise<ApiResponse<ConnectionRequestDto[]>> => apiClient.get('/api/community/networking/requests/me'),
  respondToRequest: (id: string, status: 'Accepted' | 'Declined'): Promise<ApiResponse<ConnectionRequestDto>> => apiClient.post(`/api/community/networking/requests/${id}/respond`, { status }),
  adminGetApplications: (filters?: { role?: string; status?: string; search?: string }): Promise<ApiResponse<MentorshipApplicationDto[]>> => {
    const params = new URLSearchParams();
    if (filters?.role) params.set('role', filters.role);
    if (filters?.status) params.set('status', filters.status);
    if (filters?.search) params.set('search', filters.search);
    return apiClient.get(`/api/admin/mentorship/applications${params.toString() ? `?${params}` : ''}`);
  },
  adminReview: (id: string, status: 'Approved' | 'Rejected', committeeNotes?: string): Promise<ApiResponse<MentorshipApplicationDto>> => apiClient.patch(`/api/admin/mentorship/applications/${id}`, { status, committeeNotes }),
  adminGetMatches: (): Promise<ApiResponse<MentorshipMatchDto[]>> => apiClient.get('/api/admin/mentorship/matches'),
  adminCreateMatch: (mentorApplicationId: string, menteeApplicationId: string, committeeNotes?: string): Promise<ApiResponse<MentorshipMatchDto>> => apiClient.post('/api/admin/mentorship/matches', { mentorApplicationId, menteeApplicationId, committeeNotes }),
  adminUpdateMatch: (id: string, status: 'Completed' | 'Cancelled'): Promise<ApiResponse<MentorshipMatchDto>> => apiClient.patch(`/api/admin/mentorship/matches/${id}/status`, { status }),
};
