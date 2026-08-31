import { apiClient } from './client';
import type { ApiResponse, FooterLinkDto, NavigationItemDto, PageSectionDto, ServiceContentDto, StatisticDto } from './types';

export const siteContentApi = {
  getStatistics: (): Promise<ApiResponse<StatisticDto[]>> => apiClient.get<StatisticDto[]>('/api/statistics'),
  updateStatistic: (key: string, value: string): Promise<ApiResponse<StatisticDto>> =>
    apiClient.put<StatisticDto>(`/api/statistics/${encodeURIComponent(key)}?value=${encodeURIComponent(value)}`, undefined),
  getNavigation: (admin = false): Promise<ApiResponse<NavigationItemDto[]>> => apiClient.get<NavigationItemDto[]>(`/api/navigation${admin ? '/admin' : ''}`),
  createNavigation: (data: Omit<NavigationItemDto, 'id'>) => apiClient.post<NavigationItemDto>('/api/navigation', data),
  updateNavigation: (id: string, data: Partial<NavigationItemDto>) => apiClient.put<NavigationItemDto>(`/api/navigation/${id}`, data),
  deleteNavigation: (id: string) => apiClient.delete<void>(`/api/navigation/${id}`),
  getFooter: (admin = false): Promise<ApiResponse<FooterLinkDto[]>> => apiClient.get<FooterLinkDto[]>(`/api/footer${admin ? '/admin' : ''}`),
  createFooter: (data: Omit<FooterLinkDto, 'id'>) => apiClient.post<FooterLinkDto>('/api/footer', data),
  updateFooter: (id: string, data: Partial<FooterLinkDto>) => apiClient.put<FooterLinkDto>(`/api/footer/${id}`, data),
  deleteFooter: (id: string) => apiClient.delete<void>(`/api/footer/${id}`),
  getPageSections: (page?: string, admin = false): Promise<ApiResponse<PageSectionDto[]>> => apiClient.get<PageSectionDto[]>(`/api/content/sections${admin ? '/admin' : ''}${page ? `?page=${encodeURIComponent(page)}` : ''}`),
  createPageSection: (data: Omit<PageSectionDto, 'id'>) => apiClient.post<PageSectionDto>('/api/content/sections', data),
  updatePageSection: (id: string, data: Partial<PageSectionDto>) => apiClient.put<PageSectionDto>(`/api/content/sections/${id}`, data),
  deletePageSection: (id: string) => apiClient.delete<void>(`/api/content/sections/${id}`),
  getServices: (admin = false): Promise<ApiResponse<ServiceContentDto[]>> => apiClient.get<ServiceContentDto[]>(`/api/content/services${admin ? '/admin' : ''}`),
  createService: (data: Omit<ServiceContentDto, 'id'>) => apiClient.post<ServiceContentDto>('/api/content/services', data),
  updateService: (id: string, data: Partial<ServiceContentDto>) => apiClient.put<ServiceContentDto>(`/api/content/services/${id}`, data),
  deleteService: (id: string) => apiClient.delete<void>(`/api/content/services/${id}`),
};
