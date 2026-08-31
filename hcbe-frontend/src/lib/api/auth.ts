import { apiClient } from './client';
import type { AuthResponse, LoginRequest, RegisterRequest, User, ApiResponse } from './types';

export const authApi = {
  async login(email: string, password: string): Promise<ApiResponse<AuthResponse>> {
    const request: LoginRequest = { email, password };
    return apiClient.post<AuthResponse>('/api/auth/login', request, false);
  },

  async register(data: RegisterRequest): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post<AuthResponse>('/api/auth/register', data, false);
  },

  async getCurrentUser(): Promise<ApiResponse<User>> {
    return apiClient.get<User>('/api/auth/me');
  },

  async requestPasswordReset(email: string): Promise<ApiResponse<void>> {
    return apiClient.post<void>('/api/auth/password-reset/request', { email }, false);
  },

  async confirmPasswordReset(token: string, password: string): Promise<ApiResponse<void>> {
    return apiClient.post<void>('/api/auth/password-reset/confirm', { token, password }, false);
  },

  logout(): void {
    void apiClient.post<void>('/api/auth/logout', undefined, false).catch(() => undefined);
    localStorage.removeItem('hcbe_token');
    localStorage.removeItem('hcbe_user');
  }
};
