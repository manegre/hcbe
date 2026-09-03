import { apiClient } from './client';
import type { ApiResponse, Opportunity, OpportunityApplication, UpsertOpportunityRequest } from './types';
export const opportunitiesApi = {
  getPublished: (type?: string): Promise<ApiResponse<Opportunity[]>> => apiClient.get<Opportunity[]>(`/api/opportunities${type ? `?type=${encodeURIComponent(type)}` : ''}`),
  getMine: (): Promise<ApiResponse<OpportunityApplication[]>> => apiClient.get<OpportunityApplication[]>('/api/opportunities/applications/me'),
  apply: (id: string, message: string): Promise<ApiResponse<OpportunityApplication>> => apiClient.post<OpportunityApplication>(`/api/opportunities/${id}/apply`, { message }),
  getAdmin: (): Promise<ApiResponse<Opportunity[]>> => apiClient.get<Opportunity[]>('/api/admin/opportunities'),
  create: (data: UpsertOpportunityRequest): Promise<ApiResponse<Opportunity>> => apiClient.post<Opportunity>('/api/admin/opportunities', data),
  update: (id: string, data: UpsertOpportunityRequest): Promise<ApiResponse<Opportunity>> => apiClient.put<Opportunity>(`/api/admin/opportunities/${id}`, data),
  close: (id: string): Promise<ApiResponse<void>> => apiClient.delete<void>(`/api/admin/opportunities/${id}`),
  getApplications: (): Promise<ApiResponse<OpportunityApplication[]>> => apiClient.get<OpportunityApplication[]>('/api/admin/opportunities/applications'),
  review: (id: string, status: OpportunityApplication['status'], adminNotes?: string): Promise<ApiResponse<OpportunityApplication>> => apiClient.put<OpportunityApplication>(`/api/admin/opportunities/applications/${id}`, { status, adminNotes }),
};
