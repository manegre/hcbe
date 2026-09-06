import { apiClient } from './client';
import type { ApiResponse, EventPromoCode, EventTicket, EventTicketOrder, EventTicketTier, TicketCheckout, TicketingDashboard } from './types';

export interface TicketTierInput {
  name: string; nameEn?: string; description?: string; descriptionEn?: string; priceCents: number; currency: string;
  quantity: number; maxPerOrder: number; salesStartUtc?: string; salesEndUtc?: string; isActive: boolean; displayOrder: number;
}

export const eventCommerceApi = {
  getTiers: (eventId: string) => apiClient.get<EventTicketTier[]>(`/api/event-commerce/events/${eventId}/tiers`),
  checkout: (eventId: string, data: { buyerName: string; buyerEmail: string; promoCode?: string; items: { tierId: string; quantity: number }[] }) =>
    apiClient.post<TicketCheckout>(`/api/event-commerce/events/${eventId}/checkout`, data),
  getOrder: (token: string) => apiClient.get<EventTicketOrder>(`/api/event-commerce/orders/${token}`),
  transfer: (token: string, ticketId: string, attendeeName: string, attendeeEmail: string) =>
    apiClient.put<EventTicket>(`/api/event-commerce/orders/${token}/tickets/${ticketId}/transfer`, { attendeeName, attendeeEmail }),
  getAdminTiers: (eventId: string) => apiClient.get<EventTicketTier[]>(`/api/admin/event-commerce/events/${eventId}/tiers`),
  createTier: (eventId: string, data: TicketTierInput) => apiClient.post<EventTicketTier>(`/api/admin/event-commerce/events/${eventId}/tiers`, data),
  updateTier: (eventId: string, tierId: string, data: TicketTierInput) => apiClient.put<EventTicketTier>(`/api/admin/event-commerce/events/${eventId}/tiers/${tierId}`, data),
  deleteTier: (eventId: string, tierId: string) => apiClient.delete<void>(`/api/admin/event-commerce/events/${eventId}/tiers/${tierId}`),
  getPromoCodes: (eventId: string) => apiClient.get<EventPromoCode[]>(`/api/admin/event-commerce/events/${eventId}/promo-codes`),
  createPromoCode: (eventId: string, data: Record<string, unknown>) => apiClient.post<EventPromoCode>(`/api/admin/event-commerce/events/${eventId}/promo-codes`, data),
  deletePromoCode: (eventId: string, promoId: string) => apiClient.delete<void>(`/api/admin/event-commerce/events/${eventId}/promo-codes/${promoId}`),
  getDashboard: (eventId: string) => apiClient.get<TicketingDashboard>(`/api/admin/event-commerce/events/${eventId}/dashboard`),
  checkIn: (eventId: string, code: string) => apiClient.post<EventTicket>(`/api/admin/event-commerce/events/${eventId}/check-in/${encodeURIComponent(code)}`),
  refund: (orderId: string, amountCents?: number, reason?: string): Promise<ApiResponse<EventTicketOrder>> =>
    apiClient.post<EventTicketOrder>(`/api/admin/event-commerce/orders/${orderId}/refund`, { amountCents, reason }),
};
