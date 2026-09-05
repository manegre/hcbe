import { apiClient } from './client';
import { getApiBaseUrl } from './base-url';
import type {
  ApiResponse,
  NewsletterSubscriptionDto,
  SubscribeNewsletterRequest,
  UpdateNewsletterSubscriptionRequest,
  NewsletterCampaignDto,
  CreateNewsletterCampaignRequest,
  PagedResult,
  CommunicationConsentEventDto,
  CampaignAudiencePreviewDto,
  CampaignDeliveryDto,
} from './types';

const getAuthToken = (): string | null => localStorage.getItem('hcbe_token');

export const newsletterApi = {
  subscribe: (data: SubscribeNewsletterRequest): Promise<ApiResponse<object>> =>
    apiClient.post<object>('/api/newsletter/subscribe', data, false),

  getAll: (params?: {
    language?: string;
    isActive?: boolean;
  }): Promise<ApiResponse<NewsletterSubscriptionDto[]>> => {
    const search = new URLSearchParams();
    if (params?.language) search.set('language', params.language);
    if (params?.isActive !== undefined) search.set('isActive', String(params.isActive));
    const query = search.toString() ? `?${search.toString()}` : '';
    return apiClient.get<NewsletterSubscriptionDto[]>(`/api/newsletter/subscriptions${query}`);
  },

  searchSubscriptions: (params: { page: number; pageSize?: number; search?: string; sort?: string; language?: string; isActive?: boolean }): Promise<ApiResponse<PagedResult<NewsletterSubscriptionDto>>> => {
    const search = new URLSearchParams({ page: String(params.page), pageSize: String(params.pageSize ?? 15) });
    if (params.search) search.set('search', params.search);
    if (params.sort) search.set('sort', params.sort);
    if (params.language) search.set('language', params.language);
    if (params.isActive !== undefined) search.set('isActive', String(params.isActive));
    return apiClient.get<PagedResult<NewsletterSubscriptionDto>>(`/api/newsletter/subscriptions/paged?${search}`);
  },

  updateActive: (
    id: string,
    data: UpdateNewsletterSubscriptionRequest,
  ): Promise<ApiResponse<NewsletterSubscriptionDto>> =>
    apiClient.patch<NewsletterSubscriptionDto>(`/api/newsletter/subscriptions/${id}`, data),

  getCampaigns: (): Promise<ApiResponse<NewsletterCampaignDto[]>> =>
    apiClient.get<NewsletterCampaignDto[]>('/api/newsletter/campaigns'),

  createCampaign: (data: CreateNewsletterCampaignRequest): Promise<ApiResponse<NewsletterCampaignDto>> =>
    apiClient.post<NewsletterCampaignDto>('/api/newsletter/campaigns', data),

  previewCampaign: (data: CreateNewsletterCampaignRequest): Promise<ApiResponse<CampaignAudiencePreviewDto>> =>
    apiClient.post<CampaignAudiencePreviewDto>('/api/newsletter/campaigns/preview', data),

  getCampaignDeliveries: (id: string): Promise<ApiResponse<CampaignDeliveryDto[]>> =>
    apiClient.get<CampaignDeliveryDto[]>(`/api/newsletter/campaigns/${id}/deliveries`),

  sendCampaignTest: (id: string, email: string): Promise<ApiResponse<unknown>> =>
    apiClient.post(`/api/newsletter/campaigns/${id}/test`, { email }),

  sendCampaign: (id: string): Promise<ApiResponse<NewsletterCampaignDto>> =>
    apiClient.post<NewsletterCampaignDto>(`/api/newsletter/campaigns/${id}/send`, {}),

  getConsentHistory: (limit = 100): Promise<ApiResponse<CommunicationConsentEventDto[]>> =>
    apiClient.get<CommunicationConsentEventDto[]>(`/api/newsletter/consents?limit=${limit}`),

  exportCsv: async (): Promise<Blob> => {
    const token = getAuthToken();
    const response = await fetch(`${getApiBaseUrl()}/api/newsletter/subscriptions/export`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    return response.blob();
  },
};
