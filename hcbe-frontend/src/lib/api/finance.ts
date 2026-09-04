import { apiClient } from './client';
import type { AdminMembership, ApiResponse, CheckoutResult, CheckoutSession, DonationCampaign, FinanceDashboard, FinancialTransaction, MemberFinanceSummary, MembershipPlan, MembershipStanding, MembershipVerification } from './types';

export type MembershipPlanInput = Omit<MembershipPlan, 'id' | 'benefits'> & { benefits: string[]; stripePriceId?: string };
export type CampaignInput = Omit<DonationCampaign, 'id' | 'raisedAmountCents' | 'supporterCount'>;

export const financeApi = {
  getPlans: (): Promise<ApiResponse<MembershipPlan[]>> => apiClient.get('/api/finance/plans'),
  getCampaigns: (): Promise<ApiResponse<DonationCampaign[]>> => apiClient.get('/api/finance/campaigns'),
  getMemberSummary: (): Promise<ApiResponse<MemberFinanceSummary>> => apiClient.get('/api/finance/member/summary'),
  createMembershipCheckout: (planId: string): Promise<ApiResponse<CheckoutSession>> => apiClient.post('/api/finance/member/membership/checkout', { planId }),
  createDonationCheckout: (data: { campaignId?: string; amountCents: number; currency: string; email: string; name?: string; isAnonymous: boolean; allowPublicRecognition: boolean; message?: string; isRecurring: boolean }): Promise<ApiResponse<CheckoutSession>> => apiClient.post('/api/finance/donations/checkout', data),
  getCheckoutResult: (sessionId: string): Promise<ApiResponse<CheckoutResult>> => apiClient.get(`/api/finance/checkout/${encodeURIComponent(sessionId)}`),
  createBillingPortal: (): Promise<ApiResponse<{ url: string }>> => apiClient.post('/api/finance/member/billing-portal'),
  verifyMembership: (code: string): Promise<ApiResponse<MembershipVerification>> => apiClient.get(`/api/finance/membership/verify/${encodeURIComponent(code)}`),
  adminDashboard: (): Promise<ApiResponse<FinanceDashboard>> => apiClient.get('/api/admin/finance/dashboard'),
  adminMemberships: (search = ''): Promise<ApiResponse<AdminMembership[]>> => apiClient.get(`/api/admin/finance/memberships${search ? `?search=${encodeURIComponent(search)}` : ''}`),
  adminTransactions: (query = ''): Promise<ApiResponse<FinancialTransaction[]>> => apiClient.get(`/api/admin/finance/transactions${query}`),
  adminPlans: (): Promise<ApiResponse<MembershipPlan[]>> => apiClient.get('/api/admin/finance/plans'),
  createPlan: (data: MembershipPlanInput): Promise<ApiResponse<MembershipPlan>> => apiClient.post('/api/admin/finance/plans', data),
  updatePlan: (id: string, data: MembershipPlanInput): Promise<ApiResponse<MembershipPlan>> => apiClient.put(`/api/admin/finance/plans/${id}`, data),
  adminCampaigns: (): Promise<ApiResponse<DonationCampaign[]>> => apiClient.get('/api/admin/finance/campaigns'),
  createCampaign: (data: CampaignInput): Promise<ApiResponse<DonationCampaign>> => apiClient.post('/api/admin/finance/campaigns', data),
  updateCampaign: (id: string, data: CampaignInput): Promise<ApiResponse<DonationCampaign>> => apiClient.put(`/api/admin/finance/campaigns/${id}`, data),
  refund: (id: string, amountCents?: number, reason?: string): Promise<ApiResponse<FinancialTransaction>> => apiClient.post(`/api/admin/finance/transactions/${id}/refund`, { amountCents, reason }),
  updateStanding: (userId: string, status: string, currentPeriodEndUtc?: string, note?: string): Promise<ApiResponse<MembershipStanding>> => apiClient.put(`/api/admin/finance/members/${userId}/standing`, { status, currentPeriodEndUtc, note }),
};
