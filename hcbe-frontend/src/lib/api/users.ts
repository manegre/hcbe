import { apiClient } from './client';
import type {
  AdminUser,
  ApiResponse,
  CreateAdminUserRequest,
  UpdateAdminUserRequest,
} from './types';

export const usersApi = {
  getAdminUsers: (): Promise<ApiResponse<AdminUser[]>> =>
    apiClient.get<AdminUser[]>('/api/users/admin'),

  getAdminUser: (id: string): Promise<ApiResponse<AdminUser>> =>
    apiClient.get<AdminUser>(`/api/users/admin/${id}`),

  createAdminUser: (data: CreateAdminUserRequest): Promise<ApiResponse<AdminUser>> =>
    apiClient.post<AdminUser>('/api/users/admin', data),

  updateAdminUser: (id: string, data: UpdateAdminUserRequest): Promise<ApiResponse<AdminUser>> =>
    apiClient.put<AdminUser>(`/api/users/admin/${id}`, data),

  deleteAdminUser: (id: string): Promise<ApiResponse<boolean>> =>
    apiClient.delete<boolean>(`/api/users/admin/${id}`),
};
