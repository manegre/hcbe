import { apiClient } from './client';
import { getApiBaseUrl } from './base-url';
import type {
  ApiResponse,
  CreateNewsRequest,
  MediaUpload,
  NewsArticle,
  NewsAttachment,
} from './types';

const getAuthToken = (): string | null => localStorage.getItem('hcbe_token');

const uploadMultipart = async <T>(endpoint: string, file: File): Promise<ApiResponse<T>> => {
  const formData = new FormData();
  formData.append('file', file);

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

export const newsApi = {
  getPublishedNews: (): Promise<ApiResponse<NewsArticle[]>> =>
    apiClient.get<NewsArticle[]>('/api/news'),

  getNewsById: (id: string): Promise<ApiResponse<NewsArticle>> =>
    apiClient.get<NewsArticle>(`/api/news/${id}`),

  getNewsForAdmin: (): Promise<ApiResponse<NewsArticle[]>> =>
    apiClient.get<NewsArticle[]>('/api/news/admin'),

  getNewsByIdForAdmin: (id: string): Promise<ApiResponse<NewsArticle>> =>
    apiClient.get<NewsArticle>(`/api/news/admin/${id}`),

  createNews: (data: CreateNewsRequest): Promise<ApiResponse<NewsArticle>> =>
    apiClient.post<NewsArticle>('/api/news', data),

  updateNews: (id: string, data: CreateNewsRequest): Promise<ApiResponse<NewsArticle>> =>
    apiClient.put<NewsArticle>(`/api/news/${id}`, data),

  deleteNews: (id: string): Promise<ApiResponse<void>> =>
    apiClient.delete<void>(`/api/news/${id}`),

  uploadMedia: (file: File): Promise<ApiResponse<MediaUpload>> =>
    uploadMultipart<MediaUpload>('/api/media/upload', file),

  uploadCover: (id: string, file: File): Promise<ApiResponse<MediaUpload>> =>
    uploadMultipart<MediaUpload>(`/api/news/${id}/cover`, file),

  uploadAttachment: (id: string, file: File): Promise<ApiResponse<NewsAttachment>> =>
    uploadMultipart<NewsAttachment>(`/api/news/${id}/attachments`, file),

  deleteAttachment: (id: string, attachmentId: string): Promise<ApiResponse<void>> =>
    apiClient.delete<void>(`/api/news/${id}/attachments/${attachmentId}`),
};
