import { apiClient } from './client';
import type { AdvertisingCampaign, CommunityOrganizer, OrganizerEvent } from './types';

export interface OrganizerInput { displayName: string; displayNameEn?: string; contactEmail: string; contactPhone?: string; websiteUrl?: string; description?: string; descriptionEn?: string; }
export interface AdvertisingInput { advertiserName: string; contactEmail: string; title: string; titleEn?: string; body: string; bodyEn?: string; imageUrl?: string; destinationUrl: string; placements: string[]; targetLanguage?: string; targetProvince?: string; targetZone?: string; budgetCents: number; currency: string; startsAtUtc: string; endsAtUtc: string; }
export interface OrganizerEventInput { title: string; titleEn?: string; description: string; descriptionEn?: string; date: string; endDate?: string; location?: string; locationEn?: string; format: string; imageUrl?: string; priceCents: number; currency: string; ticketQuantity: number; }

export const communityMarketplaceApi = {
  getMyOrganizer: () => apiClient.get<CommunityOrganizer | null>('/api/community-marketplace/member/organizer'),
  saveOrganizer: (data: OrganizerInput) => apiClient.put<CommunityOrganizer>('/api/community-marketplace/member/organizer', data),
  startOnboarding: () => apiClient.post<{ url: string; alreadyComplete: boolean }>('/api/community-marketplace/member/organizer/stripe/onboarding'),
  refreshOrganizer: () => apiClient.post<CommunityOrganizer>('/api/community-marketplace/member/organizer/stripe/refresh'),
  getOrganizerEvents: () => apiClient.get<OrganizerEvent[]>('/api/community-marketplace/member/organizer/events'),
  createOrganizerEvent: (data: OrganizerEventInput) => apiClient.post<OrganizerEvent>('/api/community-marketplace/member/organizer/events', data),
  createAd: (data: AdvertisingInput) => apiClient.post<AdvertisingCampaign>('/api/community-marketplace/member/ads', data),
  getMyAds: () => apiClient.get<AdvertisingCampaign[]>('/api/community-marketplace/member/ads'),
  getAds: (placement: string, language: string, province?: string, zone?: string) => { const query = new URLSearchParams({ placement, language }); if (province) query.set('province', province); if (zone) query.set('zone', zone); return apiClient.get<AdvertisingCampaign[]>(`/api/community-marketplace/ads?${query}`); },
  getAdminOrganizers: () => apiClient.get<CommunityOrganizer[]>('/api/admin/community-marketplace/organizers'),
  reviewOrganizer: (id: string, status: string, reviewNotes?: string) => apiClient.patch<CommunityOrganizer>(`/api/admin/community-marketplace/organizers/${id}`, { status, reviewNotes }),
  getAdminAds: () => apiClient.get<AdvertisingCampaign[]>('/api/admin/community-marketplace/ads'),
  reviewAd: (id: string, status: string, reviewNotes?: string) => apiClient.patch<AdvertisingCampaign>(`/api/admin/community-marketplace/ads/${id}`, { status, reviewNotes }),
};
