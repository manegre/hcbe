import { apiClient } from './client';
import type { ApiResponse, CreateTeamMemberRequest, UpdateTeamMemberRequest, TeamMemberDto } from './types';

export const teamMembersApi = {
  // Public endpoints
  getActiveTeamMembers: (): Promise<ApiResponse<TeamMemberDto[]>> =>
    apiClient.get<TeamMemberDto[]>('/api/team-members'),

  getTeamMemberById: (id: string): Promise<ApiResponse<TeamMemberDto>> =>
    apiClient.get<TeamMemberDto>(`/api/team-members/${id}`),

  // Admin endpoints
  getAllTeamMembers: (): Promise<ApiResponse<TeamMemberDto[]>> =>
    apiClient.get<TeamMemberDto[]>('/api/team-members/admin'),

  createTeamMember: (data: CreateTeamMemberRequest): Promise<ApiResponse<TeamMemberDto>> =>
    apiClient.post<TeamMemberDto>('/api/team-members', data),

  updateTeamMember: (id: string, data: UpdateTeamMemberRequest): Promise<ApiResponse<TeamMemberDto>> =>
    apiClient.put<TeamMemberDto>(`/api/team-members/${id}`, data),

  deleteTeamMember: (id: string): Promise<ApiResponse<boolean>> =>
    apiClient.delete<boolean>(`/api/team-members/${id}`),

  toggleTeamMemberStatus: (id: string): Promise<ApiResponse<boolean>> =>
    apiClient.post<boolean>(`/api/team-members/${id}/toggle-status`, {})
};