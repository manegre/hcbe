import { apiClient } from './client';
import type {
  ApiResponse,
  Consultation,
  CreateConsultationRequest,
  UpdateConsultationRequest,
} from './types';

export const consultationsApi = {
  getActiveConsultations: (): Promise<ApiResponse<Consultation[]>> =>
    apiClient.get<Consultation[]>('/api/consultations'),

  getConsultationById: (id: string): Promise<ApiResponse<Consultation>> =>
    apiClient.get<Consultation>(`/api/consultations/${id}`),

  getConsultationsForAdmin: (): Promise<ApiResponse<Consultation[]>> =>
    apiClient.get<Consultation[]>('/api/consultations/admin'),

  getConsultationForAdmin: (id: string): Promise<ApiResponse<Consultation>> =>
    apiClient.get<Consultation>(`/api/consultations/admin/${id}`),

  createConsultation: (data: CreateConsultationRequest): Promise<ApiResponse<Consultation>> =>
    apiClient.post<Consultation>('/api/consultations', data),

  updateConsultation: (id: string, data: UpdateConsultationRequest): Promise<ApiResponse<Consultation>> =>
    apiClient.put<Consultation>(`/api/consultations/${id}`, data),

  deleteConsultation: (id: string): Promise<ApiResponse<boolean>> =>
    apiClient.delete<boolean>(`/api/consultations/${id}`),

  toggleConsultationStatus: (id: string): Promise<ApiResponse<boolean>> =>
    apiClient.post<boolean>(`/api/consultations/${id}/toggle-status`, {}),
};
