import { apiClient } from './client';
import type {
  ApiResponse,
  CreateEventCategoryRequest,
  EventCategory,
  UpdateEventCategoryRequest,
} from './types';

export const eventCategoriesApi = {
  getCategories: (): Promise<ApiResponse<EventCategory[]>> =>
    apiClient.get<EventCategory[]>('/api/event-categories'),

  getCategoriesForAdmin: (): Promise<ApiResponse<EventCategory[]>> =>
    apiClient.get<EventCategory[]>('/api/event-categories/admin'),

  createCategory: (data: CreateEventCategoryRequest): Promise<ApiResponse<EventCategory>> =>
    apiClient.post<EventCategory>('/api/event-categories', data),

  updateCategory: (
    id: string,
    data: UpdateEventCategoryRequest,
  ): Promise<ApiResponse<EventCategory>> =>
    apiClient.put<EventCategory>(`/api/event-categories/${id}`, data),

  deleteCategory: (id: string): Promise<ApiResponse<void>> =>
    apiClient.delete<void>(`/api/event-categories/${id}`),
};
