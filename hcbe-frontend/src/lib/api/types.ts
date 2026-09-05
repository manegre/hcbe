// API Types
export interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  isAdmin: boolean;
  memberId?: string;
  mustChangePassword: boolean;
  adminRole?: string;
  permissions?: string[];
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message?: string;
  errors?: string[] | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface AdminUser {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  isAdmin: boolean;
  mustChangePassword: boolean;
  memberId?: string;
  createdAt: string;
  adminRole: string;
  permissions: string[];
}

export interface MemberPreference {
  preferredLanguage: 'fr' | 'en';
  timeZone: string;
  emailEvents: boolean;
  emailOpportunities: boolean;
  emailMentorship: boolean;
  emailServiceUpdates: boolean;
  emailNewsletter: boolean;
  pushNotifications: boolean;
  digestFrequency: 'Off' | 'Weekly';
  lastDigestSentAtUtc?: string;
  hasCompletedPreferences: boolean;
  updatedAt: string;
}

export interface OnboardingStep {
  key: string;
  title: string;
  completed: boolean;
  actionUrl: string;
}

export interface MemberOnboarding {
  completionPercent: number;
  isComplete: boolean;
  steps: OnboardingStep[];
  preferences: MemberPreference;
}

export type UpdateMemberPreferenceRequest = Omit<MemberPreference, 'hasCompletedPreferences' | 'updatedAt' | 'lastDigestSentAtUtc'>;

export interface AppNotification {
  id: string; type: string; title: string; message: string; relatedEntityId?: string;
  link?: string; isRead: boolean; userId?: string; createdAt: string; readAt?: string;
}

export interface SavedMemberItem {
  id: string; entityType: 'Event' | 'Opportunity'; entityId: string; title: string;
  titleEn?: string; subtitle?: string; occursAtUtc?: string; createdAtUtc: string;
}

export interface MemberDashboardEvent {
  id: string; title: string; titleEn?: string; date: string; location?: string;
  registrationStatus: string; confirmationCode: string;
}

export interface MemberDashboardOpportunity {
  id: string; title: string; titleEn?: string; type: string; organization: string;
  location?: string; isRemote: boolean; deadlineUtc?: string;
}

export interface MemberEngagementDashboard {
  memberName: string; membershipStatus: string; unreadNotifications: number; unreadMessages: number;
  openServiceCases: number; upcomingEvents: MemberDashboardEvent[];
  opportunities: MemberDashboardOpportunity[]; savedItems: SavedMemberItem[];
  recentNotifications: AppNotification[];
}

export interface MemberBlock { id: string; memberId: string; memberName: string; createdAtUtc: string; }

export interface PrivacyRequest {
  id: string;
  type: string;
  status: 'Pending' | 'Cancelled' | 'Completed' | 'Failed';
  requestedAtUtc: string;
  executeAfterUtc: string;
  cancelledAtUtc?: string | null;
  completedAtUtc?: string | null;
}

export interface AdminRole {
  key: string;
  name: string;
  permissions: string[];
}

export interface CreateAdminUserRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  adminRole?: string;
  permissions?: string[];
}

export interface UpdateAdminUserRequest {
  firstName?: string;
  lastName?: string;
  password?: string;
  isAdmin?: boolean;
  adminRole?: string;
  permissions?: string[];
}

export interface EventMedia {
  id: string;
  mediaType: 'image' | 'video' | string;
  url: string;
  fileName?: string;
  contentType?: string;
  sizeBytes?: number;
  caption?: string;
  captionEn?: string;
  displayOrder: number;
  createdAt: string;
}

export interface EventAttachment {
  id: string;
  fileName: string;
  url: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}

export interface Event {
  id: string;
  title: string;
  titleEn?: string;
  description?: string;
  descriptionEn?: string;
  date: string;
  endDate?: string;
  timeZone: string;
  location?: string;
  locationEn?: string;
  type?: string;
  format: 'InPerson' | 'Online' | 'Hybrid';
  zone?: string;
  capacity?: number;
  registrationDeadline?: string;
  meetingLink?: string;
  registrationUrl?: string;
  ctaLabel?: string;
  ctaLabelEn?: string;
  imageUrl?: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  speakers: string[];
  organizers: string[];
  media?: EventMedia[];
  attachments?: EventAttachment[];
  registrationMode: 'Disabled' | 'External' | 'Native';
  allowWaitlist: boolean;
  restrictMeetingLinkToRegistrants: boolean;
  confirmedRegistrationCount: number;
  waitlistCount: number;
  remainingCapacity?: number;
}

export type EventRegistrationStatus = 'Confirmed' | 'Waitlisted' | 'Cancelled' | 'Attended' | 'NoShow';

export interface EventRegistration {
  id: string;
  eventId: string;
  eventTitle: string;
  memberId: string;
  memberName: string;
  memberEmail: string;
  status: EventRegistrationStatus;
  confirmationCode: string;
  accessibilityNeeds?: string;
  adminNotes?: string;
  waitlistPosition?: number;
  registeredAt: string;
  updatedAt: string;
  cancelledAt?: string;
  checkedInAt?: string;
  meetingLink?: string;
}

export interface EventAttendanceStats {
  total: number; confirmed: number; waitlisted: number; attended: number; noShow: number;
  cancelled: number; attendanceRate: number; averageRating: number; surveyResponses: number;
}

export interface EventSurveyResponse {
  id: string; eventRegistrationId: string; rating: number; feedback?: string;
  consentToQuote: boolean; submittedAtUtc: string; updatedAtUtc: string;
}

export interface EventCommunication {
  id: string; audience: string; subject: string; body: string; recipientCount: number; sentAtUtc: string;
}

export interface ServiceCaseMessage { id: string; authorUserId: string; authorName: string; body: string; isInternal: boolean; createdAt: string; }
export interface ServiceCaseAttachment { id: string; fileName: string; url: string; contentType: string; sizeBytes: number; isInternal: boolean; createdAt: string; }
export interface ServiceCase {
  id: string;
  ticketNumber: string;
  memberId: string;
  memberName: string;
  memberEmail: string;
  category: string;
  subject: string;
  description: string;
  status: 'Submitted' | 'InReview' | 'AwaitingMember' | 'Resolved' | 'Closed';
  priority: 'Low' | 'Normal' | 'High' | 'Urgent';
  assignedToUserId?: string;
  assignedToName?: string;
  internalNotes?: string;
  assignedAssociationId?: string;
  assignedAssociationName?: string;
  createdAt: string;
  updatedAt: string;
  lastResponseAt?: string;
  resolvedAt?: string;
  messages: ServiceCaseMessage[];
  attachments: ServiceCaseAttachment[];
}

export interface CreateEventRequest {
  title: string;
  titleEn?: string;
  description?: string;
  descriptionEn?: string;
  date: string;
  endDate?: string;
  timeZone?: string;
  location?: string;
  locationEn?: string;
  type?: string;
  format?: 'InPerson' | 'Online' | 'Hybrid';
  zone?: string;
  capacity?: number;
  registrationDeadline?: string;
  meetingLink?: string;
  registrationUrl?: string;
  ctaLabel?: string;
  ctaLabelEn?: string;
  imageUrl?: string;
  status: string;
  speakers?: string[];
  organizers?: string[];
  registrationMode?: 'Disabled' | 'External' | 'Native';
  allowWaitlist?: boolean;
  restrictMeetingLinkToRegistrants?: boolean;
}

export interface UpdateEventRequest {
  title?: string;
  titleEn?: string;
  description?: string;
  descriptionEn?: string;
  date?: string;
  endDate?: string;
  timeZone?: string;
  location?: string;
  locationEn?: string;
  type?: string;
  format?: 'InPerson' | 'Online' | 'Hybrid';
  zone?: string;
  capacity?: number;
  registrationDeadline?: string;
  meetingLink?: string;
  registrationUrl?: string;
  ctaLabel?: string;
  ctaLabelEn?: string;
  imageUrl?: string;
  status?: string;
  speakers?: string[];
  organizers?: string[];
  registrationMode?: 'Disabled' | 'External' | 'Native';
  allowWaitlist?: boolean;
  restrictMeetingLinkToRegistrants?: boolean;
}

export interface EventCategory {
  id: string;
  slug: string;
  name: string;
  nameEn?: string;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEventCategoryRequest {
  name: string;
  nameEn?: string;
  slug?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export interface UpdateEventCategoryRequest {
  name?: string;
  nameEn?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export interface Association {
  id: string;
  name: string;
  nameEn?: string;
  description?: string;
  descriptionEn?: string;
  province: string;
  city: string;
  contact?: string;
  phone?: string;
  president?: string;
  memberCount?: string;
  foundedYear?: number;
  imageUrl?: string;
  website?: string;
  domains: string[];
  domainsEn?: string[];
  organizationType: 'Association' | 'Committee';
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAssociationRequest {
  name: string;
  nameEn?: string;
  description?: string;
  descriptionEn?: string;
  province: string;
  city: string;
  contact?: string;
  phone?: string;
  president?: string;
  memberCount?: string;
  foundedYear?: number;
  imageUrl?: string;
  website?: string;
  domains: string[];
  domainsEn?: string[];
  organizationType?: 'Association' | 'Committee';
}

export interface UpdateAssociationRequest {
  name?: string;
  nameEn?: string;
  description?: string;
  descriptionEn?: string;
  province?: string;
  city?: string;
  contact?: string;
  phone?: string;
  president?: string;
  memberCount?: string;
  foundedYear?: number;
  imageUrl?: string;
  website?: string;
  domains?: string[];
  domainsEn?: string[];
  organizationType?: 'Association' | 'Committee';
  isActive?: boolean;
}

export type AssociationPermission = 'workspace.view' | 'profile.manage' | 'members.manage' | 'documents.manage' | 'calendar.manage' | 'service-cases.manage';
export interface AssociationAccess { role: 'Owner' | 'Manager' | 'Editor' | 'Member'; title?: string; permissions: AssociationPermission[]; }
export interface AssociationMember { id: string; memberId: string; memberName: string; memberEmail: string; role: AssociationAccess['role']; title?: string; permissions: AssociationPermission[]; status: 'Active' | 'Inactive'; joinedAt: string; updatedAt: string; }
export interface AssociationJoinRequest { id: string; associationId: string; memberId: string; memberName: string; memberEmail: string; message: string; status: 'Pending' | 'Approved' | 'Rejected'; reviewNotes?: string; createdAt: string; updatedAt: string; reviewedAt?: string; }
export interface AssociationDocument { id: string; title: string; titleEn?: string; description?: string; descriptionEn?: string; fileName: string; url: string; contentType: string; sizeBytes: number; visibility: 'Members' | 'Managers'; createdAt: string; }
export interface AssociationCalendarItem { id: string; title: string; titleEn?: string; description?: string; descriptionEn?: string; location?: string; locationEn?: string; startsAtUtc: string; endsAtUtc?: string; createdAt: string; updatedAt: string; }
export interface AssociationWorkspace { association: Association; access: AssociationAccess; members: AssociationMember[]; joinRequests: AssociationJoinRequest[]; documents: AssociationDocument[]; calendarItems: AssociationCalendarItem[]; serviceCases: ServiceCase[]; }
export interface AssociationMemberMutation { role: AssociationAccess['role']; title?: string; permissions?: AssociationPermission[]; status?: 'Active' | 'Inactive'; }
export interface AssociationCalendarMutation { title: string; titleEn?: string; description?: string; descriptionEn?: string; location?: string; locationEn?: string; startsAtUtc: string; endsAtUtc?: string; }

// Project types
export interface Project {
  id: string;
  title: string;
  titleEn?: string;
  location: string;
  locationEn?: string;
  type: string; // "Développement au Burkina", "Initiative Locale"
  status: string; // "En cours", "Actif", "Planification", "Terminé"
  progress: number; // 0-100
  description: string;
  descriptionEn?: string;
  imageUrl?: string;
  budget: string;
  fundsRaised: string;
  beneficiaries: string;
  beneficiariesEn?: string;
  startDate?: string | null;
  endDate?: string | null;
  partners: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProjectRequest {
  title: string;
  titleEn?: string;
  location: string;
  locationEn?: string;
  type: string;
  status: string;
  progress: number;
  description: string;
  descriptionEn?: string;
  imageUrl?: string;
  budget: string;
  fundsRaised: string;
  beneficiaries: string;
  beneficiariesEn?: string;
  startDate?: string | null;
  endDate?: string | null;
  partners?: string[];
}

export interface UpdateProjectRequest {
  title?: string;
  titleEn?: string;
  location?: string;
  locationEn?: string;
  type?: string;
  status?: string;
  progress?: number;
  description?: string;
  descriptionEn?: string;
  imageUrl?: string;
  budget?: string;
  fundsRaised?: string;
  beneficiaries?: string;
  beneficiariesEn?: string;
  startDate?: string | null;
  endDate?: string | null;
  partners?: string[];
  isActive?: boolean;
}

// Team Members
export interface TeamMemberDto {
  id: string;
  name: string;
  position: string;
  positionEn?: string;
  region: string;
  regionEn?: string;
  zone: string;
  zoneEn?: string;
  photo?: string;
  bio?: string;
  bioEn?: string;
  email?: string;
  isActive: boolean;
  order: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTeamMemberRequest {
  name: string;
  position: string;
  positionEn?: string;
  region: string;
  regionEn?: string;
  zone: string;
  zoneEn?: string;
  photo?: string;
  bio?: string;
  bioEn?: string;
  email?: string;
  order?: number;
  isActive?: boolean;
}

export interface AssociationClaim {
  id: string;
  associationId: string;
  associationName: string;
  memberId: string;
  memberName: string;
  memberEmail: string;
  message: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  adminNotes?: string;
  createdAt: string;
  updatedAt: string;
  reviewedAt?: string;
}

export interface Opportunity {
  id: string; title: string; titleEn?: string; description: string; descriptionEn?: string;
  type: 'Volunteer' | 'Job' | 'Business' | 'Training' | 'Community'; organization: string;
  location?: string; isRemote: boolean; skills?: string; applyUrl?: string; deadlineUtc?: string;
  status: 'Draft' | 'Published' | 'Closed'; applicationCount: number; createdAt: string; updatedAt: string;
  region?: string; availability?: string; commitment?: string; requirements?: string; requirementsEn?: string;
  benefits?: string; benefitsEn?: string; contactEmail?: string; startsAtUtc?: string; endsAtUtc?: string;
}
export type UpsertOpportunityRequest = Omit<Opportunity, 'id' | 'applicationCount' | 'createdAt' | 'updatedAt'>;
export interface OpportunityMatch { opportunity: Opportunity; score: number; reasons: Array<'skills' | 'region' | 'availability' | 'remote'>; }
export interface OpportunityApplicationDocument { id: string; fileName: string; url: string; contentType: string; sizeBytes: number; createdAt: string; }
export interface VolunteerTimeEntry { id: string; activityDate: string; hours: number; description: string; status: 'Pending' | 'Approved' | 'Rejected'; reviewNotes?: string; reviewedAt?: string; createdAt: string; updatedAt: string; }
export interface OpportunityCertificate { id: string; certificateNumber: string; contributionSummary?: string; confirmedHours?: number; issuedAtUtc: string; downloadUrl: string; }
export interface OpportunityApplication { id: string; opportunityId: string; opportunityTitle: string; opportunityTitleEn?: string; opportunityType: Opportunity['type']; memberId: string; memberName: string; memberEmail: string; message: string; experience?: string; availability?: string; matchScore: number; matchReasons: string[]; documents: OpportunityApplicationDocument[]; volunteerTimeEntries: VolunteerTimeEntry[]; certificate?: OpportunityCertificate; approvedVolunteerHours: number; status: 'Submitted' | 'Reviewed' | 'Accepted' | 'Declined'; adminNotes?: string; createdAt: string; updatedAt: string; }
export interface MentorshipGoal { id: string; matchId: string; createdByMemberId: string; title: string; status: 'Open' | 'Completed' | 'Cancelled'; dueAtUtc?: string; createdAt: string; updatedAt: string; }
export interface MentorshipCheckIn { id: string; matchId: string; memberId: string; memberName: string; summary: string; rating: number; needsCommitteeSupport: boolean; createdAt: string; }
export interface MentorshipJourney { matchId: string; goals: MentorshipGoal[]; checkIns: MentorshipCheckIn[]; }
export interface ImpactMetric { key: string; label: string; value: number; changePercent?: number; unit: string; }
export interface ImpactPeriod { period: string; newMembers: number; eventRegistrations: number; serviceRequests: number; opportunityApplications: number; }
export interface ImpactDashboard { generatedAtUtc: string; metrics: ImpactMetric[]; periods: ImpactPeriod[]; }

export interface UpdateTeamMemberRequest {
  name?: string;
  position?: string;
  positionEn?: string;
  region?: string;
  regionEn?: string;
  zone?: string;
  zoneEn?: string;
  photo?: string;
  bio?: string;
  bioEn?: string;
  email?: string;
  isActive?: boolean;
  order?: number;
}

// Diaspora Members
export interface MemberDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  city?: string;
  province?: string;
  profession?: string;
  expertise?: string;
  interests?: string;
  availability?: string;
  zone?: string;
  isAdmin: boolean;
  createdAt: string;
}

export interface CreateMemberRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  city?: string;
  province?: string;
  profession?: string;
  expertise?: string;
  interests?: string;
  availability?: string;
  zone?: string;
}

export interface UpdateMemberRequest {
  firstName?: string;
  lastName?: string;
  email?: string;
  phone?: string;
  city?: string;
  province?: string;
  profession?: string;
  expertise?: string;
  interests?: string;
  availability?: string;
  zone?: string;
  isAdmin?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export type UpdateMemberAccountRequest = Omit<UpdateMemberRequest, 'email' | 'zone' | 'isAdmin'>;

// Membership Applications
export type MembershipApplicationStatus = 'Pending' | 'Approved' | 'Rejected';

export interface MembershipApplicationDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  city?: string;
  province?: string;
  profession?: string;
  expertise?: string;
  motivation?: string;
  status: MembershipApplicationStatus;
  memberId?: string;
  createdAt: string;
  reviewedAt?: string;
}

export interface CreateMembershipApplicationRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  city?: string;
  province?: string;
  profession?: string;
  expertise?: string;
  motivation?: string;
  password: string;
}

export type PublicSubmissionType =
  | 'contact'
  | 'volunteer'
  | 'event-registration'
  | 'grant-application'
  | 'consultation-response'
  | 'project-contribution';

export interface PublicSubmissionDto {
  id: string;
  type: PublicSubmissionType;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  subject?: string;
  city?: string;
  details: string;
  metadataJson?: string;
  status: 'Pending' | 'InReview' | 'Resolved' | 'Rejected';
  createdAt: string;
  reviewedAt?: string;
}

export interface CreatePublicSubmissionRequest {
  type: PublicSubmissionType;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  subject?: string;
  city?: string;
  details: string;
  metadata?: Record<string, string>;
}

// Newsletter
export interface NewsletterSubscriptionDto {
  id: string;
  email: string;
  fullName: string;
  preferredLanguage: string;
  consentAcceptedAt: string;
  isActive: boolean;
  source: string;
  createdAt: string;
  updatedAt: string;
}

export interface SubscribeNewsletterRequest {
  email: string;
  fullName: string;
  preferredLanguage: 'fr' | 'en';
  consentAccepted: boolean;
  source: 'home' | 'footer';
}

export interface UpdateNewsletterSubscriptionRequest {
  isActive: boolean;
}

export interface NewsletterCampaignDto {
  id: string;
  subject: string;
  subjectEn?: string;
  body: string;
  bodyEn?: string;
  status: 'Draft' | 'Scheduled' | 'Queued' | 'Sending' | 'Sent' | 'PartiallySent' | 'Failed';
  recipientCount: number;
  sentCount: number;
  failedCount: number;
  lastError?: string;
  createdAt: string;
  sentAt?: string;
  audience: 'Newsletter' | 'Members' | 'All';
  preferenceCategory: 'newsletter' | 'events' | 'opportunities' | 'mentorship' | 'service';
  targetProvince?: string;
  targetZone?: string;
  targetLanguage?: string;
  targetInterest?: string;
  scheduledAtUtc?: string;
  openedCount: number;
  unsubscribedCount: number;
  openRate: number;
}

export interface CommunicationConsentEventDto {
  id: string; userId?: string; email: string; category: string; action: 'OptIn' | 'OptOut'; source: string; occurredAtUtc: string;
}

export interface CreateNewsletterCampaignRequest {
  subject: string;
  subjectEn?: string;
  body: string;
  bodyEn?: string;
  audience?: 'Newsletter' | 'Members' | 'All';
  preferenceCategory?: 'newsletter' | 'events' | 'opportunities' | 'mentorship' | 'service';
  targetProvince?: string;
  targetZone?: string;
  targetLanguage?: string;
  targetInterest?: string;
  scheduledAtUtc?: string;
}

export interface StatisticDto {
  id: string;
  key: 'provinces' | 'zones' | 'associations' | 'membership' | string;
  value: string;
  label: string;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface NavigationItemDto {
  id: string;
  label: string;
  labelEn?: string;
  url: string;
  isActive: boolean;
  displayOrder: number;
}

export interface FooterLinkDto {
  id: string;
  category: string;
  categoryEn?: string;
  label: string;
  labelEn?: string;
  url: string;
  isActive: boolean;
  displayOrder: number;
}

export interface PageSectionDto {
  id: string;
  page: string;
  section: string;
  title?: string;
  titleEn?: string;
  content?: string;
  contentEn?: string;
  isActive: boolean;
  displayOrder?: number;
}

export type CmsContentType = 'text' | 'richtext' | 'image' | 'url' | 'seo';

export interface CmsPublishedContentDto {
  key: string;
  contentType: CmsContentType;
  valueFr?: string;
  valueEn?: string;
  version: number;
}

export interface CmsPublishedBundleDto {
  version: number;
  publishedAt?: string;
  items: CmsPublishedContentDto[];
}

export interface CmsContentItemDto {
  id: string;
  key: string;
  page: string;
  section: string;
  contentType: CmsContentType;
  label?: string;
  draftValueFr?: string;
  draftValueEn?: string;
  publishedValueFr?: string;
  publishedValueEn?: string;
  isPublished: boolean;
  hasUnpublishedChanges: boolean;
  version: number;
  updatedAt: string;
  publishedAt?: string;
  scheduledPublishAtUtc?: string;
}

export interface UpsertCmsContentRequest {
  key: string;
  page?: string;
  section?: string;
  contentType?: CmsContentType;
  label?: string;
  valueFr?: string;
  valueEn?: string;
  publish?: boolean;
  scheduledPublishAtUtc?: string;
}

export interface CmsContentRevisionDto {
  id: string;
  version: number;
  valueFr?: string;
  valueEn?: string;
  publishedByUserId?: string;
  publishedAt: string;
}

export interface CmsPublishResultDto {
  publishedCount: number;
  version: number;
  publishedAt: string;
}

export interface ServiceContentDto {
  id: string;
  title: string;
  titleEn?: string;
  description?: string;
  descriptionEn?: string;
  icon?: string;
  category?: string;
  categoryEn?: string;
  isActive: boolean;
  displayOrder?: number;
  details?: string;
  detailsEn?: string;
  extendedInfo?: string;
  extendedInfoEn?: string;
}

export interface PartnerDto {
  id: string;
  name: string;
  nameEn?: string;
  description?: string;
  descriptionEn?: string;
  logoUrl?: string;
  websiteUrl?: string;
  altText?: string;
  altTextEn?: string;
  isFeatured: boolean;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePartnerRequest {
  name: string;
  nameEn?: string;
  description?: string;
  descriptionEn?: string;
  logoUrl?: string;
  websiteUrl?: string;
  altText?: string;
  altTextEn?: string;
  isFeatured: boolean;
  isActive: boolean;
  displayOrder: number;
}

export type UpdatePartnerRequest = Partial<CreatePartnerRequest>;

export type MentorshipRole = 'Mentor' | 'Mentee';
export type MentorshipApplicationStatus = 'Pending' | 'Approved' | 'Rejected' | 'Matched' | 'Withdrawn';

export interface MentorshipApplicationDto {
  id: string;
  memberId: string;
  memberName: string;
  memberEmail?: string;
  role: MentorshipRole;
  professionalSummary: string;
  expertise: string;
  objectives: string;
  availability: string;
  preferredLanguage: 'fr' | 'en';
  consentToShare: boolean;
  status: MentorshipApplicationStatus;
  committeeNotes?: string;
  createdAt: string;
  updatedAt: string;
  reviewedAt?: string;
}

export interface CreateMentorshipApplicationRequest {
  role: MentorshipRole;
  professionalSummary: string;
  expertise: string;
  objectives: string;
  availability: string;
  preferredLanguage: 'fr' | 'en';
  consentToShare: boolean;
}

export interface MentorshipMatchDto {
  id: string;
  mentorApplicationId: string;
  menteeApplicationId: string;
  mentorName: string;
  menteeName: string;
  status: 'Proposed' | 'Active' | 'Declined' | 'Completed' | 'Cancelled';
  mentorAccepted: boolean;
  menteeAccepted: boolean;
  committeeNotes?: string;
  counterpartName?: string;
  counterpartEmail?: string;
  createdAt: string;
  updatedAt: string;
  activatedAt?: string;
  completedAt?: string;
}

export interface NetworkingProfileDto {
  id: string;
  memberId: string;
  memberName: string;
  headline: string;
  bio: string;
  expertise: string;
  sectors: string;
  city?: string;
  province?: string;
  isVisible: boolean;
  allowContactRequests: boolean;
  updatedAt: string;
}

export type UpsertNetworkingProfileRequest = Omit<NetworkingProfileDto, 'id' | 'memberId' | 'memberName' | 'updatedAt'>;

export interface ConnectionRequestDto {
  id: string;
  requesterMemberId: string;
  recipientMemberId: string;
  requesterName: string;
  recipientName: string;
  message: string;
  status: 'Pending' | 'Accepted' | 'Declined';
  direction: 'Sent' | 'Received';
  sharedEmail?: string;
  createdAt: string;
  respondedAt?: string;
}

export interface MessagingContactDto {
  memberId: string;
  memberName: string;
  relationshipType: 'Networking' | 'Mentorship';
  relationshipId: string;
  hasConversation: boolean;
  conversationId?: string;
}

export interface ConversationDto {
  id: string;
  counterpartMemberId: string;
  counterpartName: string;
  relationshipType: 'Networking' | 'Mentorship';
  status: 'Active' | 'Suspended';
  lastMessage?: string;
  lastMessageAt?: string;
  unreadCount: number;
  createdAt: string;
}

export interface PrivateMessageDto {
  id: string;
  conversationId: string;
  senderMemberId: string;
  senderName: string;
  body: string;
  isMine: boolean;
  createdAt: string;
  readAt?: string;
}

export interface ConversationReportDto {
  id: string;
  conversationId: string;
  reporterMemberId: string;
  reporterName: string;
  memberOneName: string;
  memberTwoName: string;
  reason: string;
  status: 'Open' | 'Resolved' | 'Dismissed';
  adminNotes?: string;
  createdAt: string;
  resolvedAt?: string;
}

// Grant Programs
export interface GrantProgram {
  id: string;
  title: string;
  titleEn?: string;
  description: string;
  descriptionEn?: string;
  icon: string;
  amount: string;
  amountEn?: string;
  duration: string;
  durationEn?: string;
  eligibilityCriteria: string[];
  eligibilityCriteriaEn?: string[];
  applicationUrl?: string;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateGrantProgramRequest {
  title: string;
  titleEn?: string;
  description: string;
  descriptionEn?: string;
  icon: string;
  amount: string;
  amountEn?: string;
  duration: string;
  durationEn?: string;
  eligibilityCriteria: string[];
  eligibilityCriteriaEn?: string[];
  applicationUrl?: string;
  displayOrder?: number;
  isActive?: boolean;
}

export interface UpdateGrantProgramRequest {
  title?: string;
  titleEn?: string;
  description?: string;
  descriptionEn?: string;
  icon?: string;
  amount?: string;
  amountEn?: string;
  duration?: string;
  durationEn?: string;
  eligibilityCriteria?: string[];
  eligibilityCriteriaEn?: string[];
  applicationUrl?: string;
  displayOrder?: number;
  isActive?: boolean;
}

// Consultations
export interface Consultation {
  id: string;
  title: string;
  titleEn?: string;
  description: string;
  descriptionEn?: string;
  icon: string;
  layoutType: 'featured' | 'card';
  actionUrl?: string;
  actionLabel?: string;
  actionLabelEn?: string;
  secondaryActionUrl?: string;
  secondaryActionLabel?: string;
  secondaryActionLabelEn?: string;
  accentColor: 'emerald' | 'amber';
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  governanceType: 'Information' | 'Survey' | 'Proposal' | 'Vote';
  opensAtUtc?: string;
  closesAtUtc?: string;
  commentClosesAtUtc?: string;
  votingMode: 'Named' | 'Anonymous';
  eligibilityRule: 'AllMembers' | 'ActiveMembers' | 'Administrators';
  quorumPercentage: number;
  minimumParticipation: number;
  allowComments: boolean;
  resultsPublishedAtUtc?: string;
  options: ConsultationOption[];
  comments: ConsultationComment[];
  governance?: ConsultationGovernance;
  selectedOptionId?: string;
}

export interface ConsultationOption { id: string; label: string; labelEn?: string; displayOrder: number }
export interface ConsultationOptionRequest { label: string; labelEn?: string }
export interface ConsultationComment { id: string; memberName: string; body: string; createdAtUtc: string }
export interface ConsultationResult extends ConsultationOptionRequest { optionId: string; voteCount: number; percentage: number }
export interface ConsultationGovernance {
  status: 'Draft' | 'Upcoming' | 'Open' | 'Closed';
  isEligible: boolean;
  hasParticipated: boolean;
  canVote: boolean;
  canComment: boolean;
  eligibleCount: number;
  participantCount: number;
  requiredParticipation: number;
  quorumReached: boolean;
  resultsPublished: boolean;
  results: ConsultationResult[];
}
export interface ConsultationAuditEvent { id: string; action: string; details?: string; actor?: string; createdAtUtc: string }

export interface CreateConsultationRequest {
  title: string;
  titleEn?: string;
  description: string;
  descriptionEn?: string;
  icon: string;
  layoutType: 'featured' | 'card';
  actionUrl?: string;
  actionLabel?: string;
  actionLabelEn?: string;
  secondaryActionUrl?: string;
  secondaryActionLabel?: string;
  secondaryActionLabelEn?: string;
  accentColor?: 'emerald' | 'amber';
  displayOrder?: number;
  isActive?: boolean;
  governanceType?: Consultation['governanceType'];
  opensAtUtc?: string;
  closesAtUtc?: string;
  commentClosesAtUtc?: string;
  votingMode?: Consultation['votingMode'];
  eligibilityRule?: Consultation['eligibilityRule'];
  quorumPercentage?: number;
  minimumParticipation?: number;
  allowComments?: boolean;
  options?: ConsultationOptionRequest[];
}

export interface UpdateConsultationRequest {
  title?: string;
  titleEn?: string;
  description?: string;
  descriptionEn?: string;
  icon?: string;
  layoutType?: 'featured' | 'card';
  actionUrl?: string;
  actionLabel?: string;
  actionLabelEn?: string;
  secondaryActionUrl?: string;
  secondaryActionLabel?: string;
  secondaryActionLabelEn?: string;
  accentColor?: 'emerald' | 'amber';
  displayOrder?: number;
  isActive?: boolean;
  governanceType?: Consultation['governanceType'];
  opensAtUtc?: string;
  closesAtUtc?: string;
  commentClosesAtUtc?: string;
  votingMode?: Consultation['votingMode'];
  eligibilityRule?: Consultation['eligibilityRule'];
  quorumPercentage?: number;
  minimumParticipation?: number;
  allowComments?: boolean;
  options?: ConsultationOptionRequest[];
}

// News / Annonces
export interface NewsAttachment {
  id: string;
  fileName: string;
  url: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}

export interface MediaUpload {
  url: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

export interface NewsArticle {
  id: string;
  title: string;
  titleEn?: string;
  content: string;
  contentEn?: string;
  excerpt?: string;
  excerptEn?: string;
  imageUrl?: string;
  imagePosition?: 'top' | 'center' | 'bottom';
  author?: string;
  category?: string;
  publishedDate?: string;
  isPinned: boolean;
  status: string;
  createdAt: string;
  updatedAt: string;
  attachments?: NewsAttachment[];
}

export interface CreateNewsRequest {
  title: string;
  titleEn?: string;
  content: string;
  contentEn?: string;
  excerpt?: string;
  excerptEn?: string;
  imageUrl?: string;
  imagePosition?: 'top' | 'center' | 'bottom';
  author?: string;
  category?: string;
  publishedDate?: string;
  isPinned?: boolean;
  status: string;
}

export interface MembershipPlan {
  id: string; name: string; nameEn?: string; description: string; descriptionEn?: string;
  amountCents: number; currency: string; billingMode: 'Annual' | 'Recurring'; benefits: string[];
  stripePriceId?: string; isActive: boolean; displayOrder: number;
}

export interface MembershipStanding {
  status: 'Inactive' | 'Active' | 'GracePeriod' | 'Expired';
  currentPeriodStartUtc?: string; currentPeriodEndUtc?: string; graceEndsAtUtc?: string;
  autoRenew: boolean; hasBillingAccount: boolean; hasActiveSubscription: boolean;
  plan?: MembershipPlan; verificationCode?: string; verificationUrl?: string;
}

export interface DonationCampaign {
  id: string; slug: string; title: string; titleEn?: string; description: string; descriptionEn?: string;
  goalAmountCents: number; raisedAmountCents: number; currency: string; imageUrl?: string;
  allowRecurring: boolean; isPublished: boolean; startsAtUtc?: string; endsAtUtc?: string; supporterCount: number;
}

export interface FinancialTransaction {
  id: string; kind: 'Membership' | 'Donation'; status: string; amountCents: number;
  refundedAmountCents: number; currency: string; payerEmail: string; payerName?: string;
  isAnonymous: boolean; allowPublicRecognition: boolean; isRecurring: boolean;
  receiptNumber: string; receiptUrl?: string; membershipPlanId?: string; donationCampaignId?: string;
  campaignTitle?: string; createdAtUtc: string; paidAtUtc?: string; refundedAtUtc?: string;
}

export interface MemberFinanceSummary {
  membership: MembershipStanding; plans: MembershipPlan[]; transactions: FinancialTransaction[];
}

export interface CheckoutSession { transactionId: string; checkoutUrl: string; sessionId: string; }
export interface CheckoutResult { status: string; kind: string; amountCents: number; currency: string; receiptUrl?: string; returnUrl?: string; }
export interface FinanceDashboard {
  paidAmountCents: number; refundedAmountCents: number; membershipRevenueCents: number;
  donationRevenueCents: number; activeMembers: number; expiringMembers: number;
  paidTransactionCount: number; recentTransactions: FinancialTransaction[];
}
export interface AdminMembership {
  userId: string; memberName: string; email: string; status: 'Inactive' | 'Active' | 'GracePeriod' | 'Expired';
  planName?: string; currentPeriodEndUtc?: string; graceEndsAtUtc?: string; autoRenew: boolean;
}
export interface MembershipVerification {
  isValid: boolean; status: string; memberName: string; planName?: string; planNameEn?: string; validUntilUtc?: string; verificationCode: string;
}
