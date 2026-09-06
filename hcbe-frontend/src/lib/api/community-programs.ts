import { apiClient } from './client';

export interface CommunityBusiness { id: string; name: string; nameEn?: string; category: string; description: string; descriptionEn?: string; services?: string; servicesEn?: string; contactEmail: string; contactPhone?: string; websiteUrl?: string; logoUrl?: string; city?: string; province?: string; serviceRegions?: string; status: string; isFeatured: boolean; reviewNotes?: string; createdAtUtc: string; updatedAtUtc: string; }
export interface NewcomerJourney { id: string; arrivalDate?: string; city?: string; province?: string; preferredLanguage: string; needs: string[]; completedSteps: string[]; mentorRequested: boolean; progressPercent: number; updatedAtUtc: string; }
export interface FamilyMember { id: string; fullName: string; relationship: string; email?: string; birthDate?: string; status: string; createdAtUtc: string; }
export interface FamilyHousehold { id: string; householdName: string; status: string; members: FamilyMember[]; updatedAtUtc: string; }
export interface AppointmentOffering { id: string; title: string; titleEn?: string; description: string; descriptionEn?: string; category: string; mode: string; location?: string; locationEn?: string; durationMinutes: number; isActive: boolean; }
export interface AppointmentSlot { id: string; offeringId: string; offeringTitle: string; offeringTitleEn?: string; startsAtUtc: string; endsAtUtc: string; capacity: number; available: number; isCancelled: boolean; }
export interface AppointmentBooking { id: string; slotId: string; offeringTitle: string; offeringTitleEn?: string; startsAtUtc: string; endsAtUtc: string; reason?: string; status: string; createdAtUtc: string; }
export interface PartnerBenefit { id: string; partnerId: string; partnerName: string; partnerLogoUrl?: string; title: string; titleEn?: string; description: string; descriptionEn?: string; terms?: string; termsEn?: string; redemptionInstructions?: string; redemptionInstructionsEn?: string; startsAtUtc?: string; endsAtUtc?: string; maxClaims?: number; claimCount: number; isActive: boolean; isClaimed: boolean; redemptionCode?: string; }
export interface GrantApplication { id: string; grantProgramId: string; programTitle: string; programTitleEn?: string; applicantName: string; applicantEmail: string; statement: string; answers: Record<string, string>; documents: string[]; status: string; adminNotes?: string; submittedAtUtc: string; updatedAtUtc: string; }
export interface SponsorshipPackage { id: string; title: string; titleEn?: string; description: string; descriptionEn?: string; deliverables: string[]; amountCents: number; currency: string; isActive: boolean; displayOrder: number; }
export interface SponsorshipRequest { id: string; packageId?: string; packageTitle?: string; organizationName: string; contactEmail: string; objective: string; notes?: string; proposedAmountCents: number; currency: string; status: string; createdAtUtc: string; updatedAtUtc: string; }
export interface AnnualReport { id: string; year: number; title: string; titleEn?: string; summary: string; summaryEn?: string; metrics: Record<string, number>; status: string; generatedAtUtc: string; publishedAtUtc?: string; }
export interface AutomationRule { id: string; key: string; name: string; nameEn: string; cadence: string; isEnabled: boolean; lastRunAtUtc?: string; nextRunAtUtc?: string; lastStatus?: string; lastSummary?: string; }
export interface ProgramsAdminOverview { businesses: CommunityBusiness[]; offerings: AppointmentOffering[]; slots: AppointmentSlot[]; bookings: AppointmentBooking[]; benefits: PartnerBenefit[]; grantApplications: GrantApplication[]; sponsorshipPackages: SponsorshipPackage[]; sponsorshipRequests: SponsorshipRequest[]; annualReports: AnnualReport[]; automationRules: AutomationRule[]; }

export interface BusinessInput { name: string; nameEn?: string; category: string; description: string; descriptionEn?: string; services?: string; servicesEn?: string; contactEmail: string; contactPhone?: string; websiteUrl?: string; logoUrl?: string; city?: string; province?: string; serviceRegions?: string; }
export interface JourneyInput { arrivalDate?: string; city?: string; province?: string; preferredLanguage: string; needs: string[]; completedSteps: string[]; mentorRequested: boolean; }

export const communityProgramsApi = {
  directory: (search = '', category = '', province = '') => apiClient.get<CommunityBusiness[]>(`/api/community-programs/directory?${new URLSearchParams({ search, category, province })}`),
  reports: () => apiClient.get<AnnualReport[]>('/api/community-programs/annual-reports'),
  sponsorshipPackages: () => apiClient.get<SponsorshipPackage[]>('/api/community-programs/sponsorship-packages'),
  myBusinesses: () => apiClient.get<CommunityBusiness[]>('/api/community-programs/member/businesses'),
  createBusiness: (data: BusinessInput) => apiClient.post<CommunityBusiness>('/api/community-programs/member/businesses', data),
  updateBusiness: (id: string, data: BusinessInput) => apiClient.put<CommunityBusiness>(`/api/community-programs/member/businesses/${id}`, data),
  journey: () => apiClient.get<NewcomerJourney | null>('/api/community-programs/member/newcomer'),
  saveJourney: (data: JourneyInput) => apiClient.put<NewcomerJourney>('/api/community-programs/member/newcomer', data),
  family: () => apiClient.get<FamilyHousehold | null>('/api/community-programs/member/family'),
  saveFamily: (householdName: string) => apiClient.put<FamilyHousehold>('/api/community-programs/member/family', { householdName }),
  addFamilyMember: (data: { fullName: string; relationship: string; email?: string; birthDate?: string }) => apiClient.post<FamilyHousehold>('/api/community-programs/member/family/members', data),
  removeFamilyMember: (id: string) => apiClient.delete<FamilyHousehold>(`/api/community-programs/member/family/members/${id}`),
  appointmentSlots: () => apiClient.get<AppointmentSlot[]>('/api/community-programs/member/appointments/slots'),
  myAppointments: () => apiClient.get<AppointmentBooking[]>('/api/community-programs/member/appointments'),
  bookAppointment: (slotId: string, reason?: string) => apiClient.post<AppointmentBooking>('/api/community-programs/member/appointments', { slotId, reason }),
  cancelAppointment: (id: string) => apiClient.post<AppointmentBooking>(`/api/community-programs/member/appointments/${id}/cancel`),
  benefits: () => apiClient.get<PartnerBenefit[]>('/api/community-programs/member/benefits'),
  claimBenefit: (id: string) => apiClient.post<PartnerBenefit>(`/api/community-programs/member/benefits/${id}/claim`),
  myGrantApplications: () => apiClient.get<GrantApplication[]>('/api/community-programs/member/grant-applications'),
  applyForGrant: (data: { grantProgramId: string; applicantName: string; applicantEmail: string; statement: string; answers?: Record<string, string>; documents?: string[] }) => apiClient.post<GrantApplication>('/api/community-programs/member/grant-applications', data),
  withdrawGrant: (id: string) => apiClient.post<GrantApplication>(`/api/community-programs/member/grant-applications/${id}/withdraw`),
  mySponsorships: () => apiClient.get<SponsorshipRequest[]>('/api/community-programs/member/sponsorships'),
  requestSponsorship: (data: { packageId?: string; organizationName: string; contactEmail: string; objective: string; proposedAmountCents: number; currency: string }) => apiClient.post<SponsorshipRequest>('/api/community-programs/member/sponsorships', data),
  adminOverview: () => apiClient.get<ProgramsAdminOverview>('/api/admin/community-programs/overview'),
  reviewBusiness: (id: string, status: string, isFeatured = false, reviewNotes?: string) => apiClient.patch<CommunityBusiness>(`/api/admin/community-programs/businesses/${id}`, { status, isFeatured, reviewNotes }),
  saveOffering: (data: Omit<AppointmentOffering, 'id'>, id?: string) => id ? apiClient.put<AppointmentOffering>(`/api/admin/community-programs/appointment-offerings/${id}`, data) : apiClient.post<AppointmentOffering>('/api/admin/community-programs/appointment-offerings', data),
  createSlot: (data: { offeringId: string; startsAtUtc: string; endsAtUtc: string; capacity: number }) => apiClient.post<AppointmentSlot>('/api/admin/community-programs/appointment-slots', data),
  saveBenefit: (data: Record<string, unknown>, id?: string) => id ? apiClient.put<PartnerBenefit>(`/api/admin/community-programs/benefits/${id}`, data) : apiClient.post<PartnerBenefit>('/api/admin/community-programs/benefits', data),
  reviewGrant: (id: string, status: string, adminNotes?: string) => apiClient.patch<GrantApplication>(`/api/admin/community-programs/grant-applications/${id}`, { status, adminNotes }),
  saveSponsorshipPackage: (data: Record<string, unknown>, id?: string) => id ? apiClient.put<SponsorshipPackage>(`/api/admin/community-programs/sponsorship-packages/${id}`, data) : apiClient.post<SponsorshipPackage>('/api/admin/community-programs/sponsorship-packages', data),
  reviewSponsorship: (id: string, status: string, notes?: string) => apiClient.patch<SponsorshipRequest>(`/api/admin/community-programs/sponsorships/${id}`, { status, notes }),
  generateReport: (year: number) => apiClient.post<AnnualReport>(`/api/admin/community-programs/annual-reports/${year}/generate`),
  publishReport: (id: string) => apiClient.post<AnnualReport>(`/api/admin/community-programs/annual-reports/${id}/publish`),
  updateAutomation: (id: string, isEnabled: boolean, cadence: string) => apiClient.put<AutomationRule>(`/api/admin/community-programs/automations/${id}`, { isEnabled, cadence }),
  runAutomations: () => apiClient.post<AutomationRule[]>('/api/admin/community-programs/automations/run'),
};
