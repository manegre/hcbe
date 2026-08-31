import { apiClient } from './client';
import { getApiBaseUrl } from './base-url';
import type {
  ApiResponse,
  Association,
  CreateAssociationRequest,
  MediaUpload,
  UpdateAssociationRequest,
} from './types';

const getAuthToken = (): string | null => localStorage.getItem('hcbe_token');

const uploadMultipart = async <T>(
  endpoint: string,
  file: File,
  extraFields?: Record<string, string>,
): Promise<ApiResponse<T>> => {
  const formData = new FormData();
  formData.append('file', file);
  if (extraFields) {
    Object.entries(extraFields).forEach(([key, value]) => {
      formData.append(key, value);
    });
  }

  const headers: HeadersInit = {};
  const token = getAuthToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${getApiBaseUrl()}${endpoint}`, {
    method: 'POST',
    headers,
    body: formData,
  });

  if (response.status === 401) {
    localStorage.removeItem('hcbe_token');
    localStorage.removeItem('hcbe_user');
    window.location.href = '/admin/login';
    throw new Error('Unauthorized');
  }

  const payload = (await response.json()) as ApiResponse<T>;
  if (!response.ok && !payload?.message) {
    throw new Error(`HTTP ${response.status}`);
  }
  return payload;
};

export const associationsApi = {
  getAssociations: async (): Promise<ApiResponse<Association[]>> => {
    return await apiClient.get<Association[]>('/api/associations');
  },

  getAssociationsForAdmin: async (): Promise<ApiResponse<Association[]>> => {
    return await apiClient.get<Association[]>('/api/admin/associations');
  },

  getAssociation: async (id: string): Promise<ApiResponse<Association>> => {
    return await apiClient.get<Association>(`/api/associations/${id}`);
  },

  getAssociationForAdmin: async (id: string): Promise<ApiResponse<Association>> => {
    return await apiClient.get<Association>(`/api/admin/associations/${id}`);
  },

  createAssociation: async (data: CreateAssociationRequest): Promise<ApiResponse<Association>> => {
    return await apiClient.post<Association>('/api/associations', data);
  },

  updateAssociation: async (
    id: string,
    data: UpdateAssociationRequest,
  ): Promise<ApiResponse<Association>> => {
    return await apiClient.put<Association>(`/api/associations/${id}`, data);
  },

  deleteAssociation: async (id: string): Promise<ApiResponse<void>> => {
    return await apiClient.delete<void>(`/api/associations/${id}`);
  },

  uploadMedia: (file: File): Promise<ApiResponse<MediaUpload>> =>
    uploadMultipart<MediaUpload>('/api/media/upload', file, { folder: 'associations' }),

  uploadImage: (id: string, file: File): Promise<ApiResponse<MediaUpload>> =>
    uploadMultipart<MediaUpload>(`/api/associations/${id}/image`, file),
};
