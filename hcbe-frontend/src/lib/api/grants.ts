import { apiClient } from './client';
import type {
  ApiResponse,
  CreateGrantProgramRequest,
  GrantProgram,
  UpdateGrantProgramRequest,
} from './types';

export const grantsApi = {
  getActiveGrants: (): Promise<ApiResponse<GrantProgram[]>> =>
    apiClient.get<GrantProgram[]>('/api/grants'),

  getGrantById: (id: string): Promise<ApiResponse<GrantProgram>> =>
    apiClient.get<GrantProgram>(`/api/grants/${id}`),

  getGrantsForAdmin: (): Promise<ApiResponse<GrantProgram[]>> =>
    apiClient.get<GrantProgram[]>('/api/grants/admin'),

  getGrantForAdmin: (id: string): Promise<ApiResponse<GrantProgram>> =>
    apiClient.get<GrantProgram>(`/api/grants/admin/${id}`),

  createGrant: (data: CreateGrantProgramRequest): Promise<ApiResponse<GrantProgram>> =>
    apiClient.post<GrantProgram>('/api/grants', data),

  updateGrant: (id: string, data: UpdateGrantProgramRequest): Promise<ApiResponse<GrantProgram>> =>
    apiClient.put<GrantProgram>(`/api/grants/${id}`, data),

  deleteGrant: (id: string): Promise<ApiResponse<boolean>> =>
    apiClient.delete<boolean>(`/api/grants/${id}`),

  toggleGrantStatus: (id: string): Promise<ApiResponse<boolean>> =>
    apiClient.post<boolean>(`/api/grants/${id}/toggle-status`, {}),
};
