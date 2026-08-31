import { apiClient } from './client';
import { getApiBaseUrl } from './base-url';
import type {
  Event,
  CreateEventRequest,
  UpdateEventRequest,
  ApiResponse,
  EventMedia,
  EventAttachment,
  MediaUpload,
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

export const eventsApi = {
  async getEvents(): Promise<ApiResponse<Event[]>> {
    return apiClient.get<Event[]>('/api/events');
  },

  async getEvent(id: string): Promise<ApiResponse<Event>> {
    return apiClient.get<Event>(`/api/events/${id}`);
  },

  async getEventsForAdmin(): Promise<ApiResponse<Event[]>> {
    return apiClient.get<Event[]>('/api/events/admin');
  },

  async getEventForAdmin(id: string): Promise<ApiResponse<Event>> {
    return apiClient.get<Event>(`/api/events/admin/${id}`);
  },

  async createEvent(data: CreateEventRequest): Promise<ApiResponse<Event>> {
    return apiClient.post<Event>('/api/events', data);
  },

  async updateEvent(id: string, data: UpdateEventRequest): Promise<ApiResponse<Event>> {
    return apiClient.put<Event>(`/api/events/${id}`, data);
  },

  async deleteEvent(id: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`/api/events/${id}`);
  },

  uploadMedia: (file: File): Promise<ApiResponse<MediaUpload>> =>
    uploadMultipart<MediaUpload>('/api/media/upload', file, { folder: 'events' }),

  uploadPhoto: (id: string, file: File): Promise<ApiResponse<EventMedia>> =>
    uploadMultipart<EventMedia>(`/api/events/${id}/media/photos`, file),

  addVideo: (
    id: string,
    url: string,
    caption?: string,
    captionEn?: string,
  ): Promise<ApiResponse<EventMedia>> =>
    apiClient.post<EventMedia>(`/api/events/${id}/media/videos`, { url, caption, captionEn }),

  deleteMedia: (id: string, mediaId: string): Promise<ApiResponse<void>> =>
    apiClient.delete<void>(`/api/events/${id}/media/${mediaId}`),

  uploadAttachment: (id: string, file: File): Promise<ApiResponse<EventAttachment>> =>
    uploadMultipart<EventAttachment>(`/api/events/${id}/attachments`, file),

  deleteAttachment: (id: string, attachmentId: string): Promise<ApiResponse<void>> =>
    apiClient.delete<void>(`/api/events/${id}/attachments/${attachmentId}`),
};
