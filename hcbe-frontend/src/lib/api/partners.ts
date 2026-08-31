import { getApiBaseUrl } from './base-url';
import { apiClient } from './client';
import type {
  ApiResponse,
  CreatePartnerRequest,
  MediaUpload,
  PartnerDto,
  UpdatePartnerRequest,
} from './types';

const uploadLogo = async (file: File): Promise<ApiResponse<MediaUpload>> => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('folder', 'partners');
  const token = localStorage.getItem('hcbe_token');
  const response = await fetch(`${getApiBaseUrl()}/api/media/upload`, {
    method: 'POST',
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    body: formData,
  });
  return (await response.json()) as ApiResponse<MediaUpload>;
};

export const partnersApi = {
  getPublic: (): Promise<ApiResponse<PartnerDto[]>> => apiClient.get('/api/partners'),
  getAdmin: (): Promise<ApiResponse<PartnerDto[]>> => apiClient.get('/api/partners/admin'),
  getById: (id: string): Promise<ApiResponse<PartnerDto>> => apiClient.get(`/api/partners/${id}`),
  create: (request: CreatePartnerRequest): Promise<ApiResponse<PartnerDto>> =>
    apiClient.post('/api/partners', request),
  update: (id: string, request: UpdatePartnerRequest): Promise<ApiResponse<PartnerDto>> =>
    apiClient.put(`/api/partners/${id}`, request),
  delete: (id: string): Promise<ApiResponse<void>> => apiClient.delete(`/api/partners/${id}`),
  reorder: (partnerIds: string[]): Promise<ApiResponse<PartnerDto[]>> =>
    apiClient.put('/api/partners/reorder', { partnerIds }),
  uploadLogo,
};
