import { apiClient } from './client';
import { getApiBaseUrl } from './base-url';
import type { ApiResponse, Opportunity, OpportunityApplication, OpportunityApplicationDocument, OpportunityCertificate, OpportunityMatch, UpsertOpportunityRequest, VolunteerTimeEntry } from './types';

const uploadDocument = async (applicationId: string, file: File): Promise<ApiResponse<OpportunityApplicationDocument>> => {
  const data = new FormData(); data.append('file', file);
  const token = localStorage.getItem('hcbe_token');
  const response = await fetch(`${getApiBaseUrl()}/api/opportunities/applications/${applicationId}/documents`, { method: 'POST', headers: token ? { Authorization: `Bearer ${token}` } : {}, body: data });
  const payload = await response.json() as ApiResponse<OpportunityApplicationDocument>;
  if (!response.ok && !payload.message) throw new Error(`HTTP ${response.status}`);
  return payload;
};

export const opportunitiesApi = {
  getPublished: (type?: string): Promise<ApiResponse<Opportunity[]>> => apiClient.get<Opportunity[]>(`/api/opportunities${type ? `?type=${encodeURIComponent(type)}` : ''}`),
  getMatched: (type?: string): Promise<ApiResponse<OpportunityMatch[]>> => apiClient.get<OpportunityMatch[]>(`/api/opportunities/matched${type ? `?type=${encodeURIComponent(type)}` : ''}`),
  getMine: (): Promise<ApiResponse<OpportunityApplication[]>> => apiClient.get<OpportunityApplication[]>('/api/opportunities/applications/me'),
  apply: (id: string, data: { message: string; experience?: string; availability?: string }): Promise<ApiResponse<OpportunityApplication>> => apiClient.post<OpportunityApplication>(`/api/opportunities/${id}/apply`, data),
  uploadDocument,
  deleteDocument: (applicationId: string, documentId: string): Promise<ApiResponse<void>> => apiClient.delete<void>(`/api/opportunities/applications/${applicationId}/documents/${documentId}`),
  downloadDocument: (applicationId: string, documentId: string) => apiClient.download(`/api/opportunities/applications/${applicationId}/documents/${documentId}/download`),
  addHours: (applicationId: string, data: { activityDate: string; hours: number; description: string }): Promise<ApiResponse<VolunteerTimeEntry>> => apiClient.post<VolunteerTimeEntry>(`/api/opportunities/applications/${applicationId}/hours`, data),
  downloadCertificate: (applicationId: string) => apiClient.download(`/api/opportunities/applications/${applicationId}/certificate`),
  getAdmin: (): Promise<ApiResponse<Opportunity[]>> => apiClient.get<Opportunity[]>('/api/admin/opportunities'),
  create: (data: UpsertOpportunityRequest): Promise<ApiResponse<Opportunity>> => apiClient.post<Opportunity>('/api/admin/opportunities', data),
  update: (id: string, data: UpsertOpportunityRequest): Promise<ApiResponse<Opportunity>> => apiClient.put<Opportunity>(`/api/admin/opportunities/${id}`, data),
  close: (id: string): Promise<ApiResponse<void>> => apiClient.delete<void>(`/api/admin/opportunities/${id}`),
  getApplications: (): Promise<ApiResponse<OpportunityApplication[]>> => apiClient.get<OpportunityApplication[]>('/api/admin/opportunities/applications'),
  review: (id: string, status: OpportunityApplication['status'], adminNotes?: string): Promise<ApiResponse<OpportunityApplication>> => apiClient.put<OpportunityApplication>(`/api/admin/opportunities/applications/${id}`, { status, adminNotes }),
  reviewHours: (id: string, status: 'Approved' | 'Rejected', reviewNotes?: string): Promise<ApiResponse<VolunteerTimeEntry>> => apiClient.put<VolunteerTimeEntry>(`/api/admin/opportunities/hours/${id}`, { status, reviewNotes }),
  issueCertificate: (applicationId: string, contributionSummary?: string): Promise<ApiResponse<OpportunityCertificate>> => apiClient.post<OpportunityCertificate>(`/api/admin/opportunities/applications/${applicationId}/certificate`, { contributionSummary }),
};
