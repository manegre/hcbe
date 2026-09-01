// API Types
export interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  isAdmin: boolean;
  memberId?: string;
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
  createdAt: string;
}

export interface CreateAdminUserRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface UpdateAdminUserRequest {
  firstName?: string;
  lastName?: string;
  password?: string;
  isAdmin?: boolean;
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
  location?: string;
  locationEn?: string;
  type?: string;
  zone?: string;
  capacity?: number;
  registrationDeadline?: string;
  meetingLink?: string;
  imageUrl?: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  media?: EventMedia[];
  attachments?: EventAttachment[];
}

export interface CreateEventRequest {
  title: string;
  titleEn?: string;
  description?: string;
  descriptionEn?: string;
  date: string;
  location?: string;
  locationEn?: string;
  type?: string;
  zone?: string;
  capacity?: number;
  registrationDeadline?: string;
  meetingLink?: string;
  imageUrl?: string;
  status: string;
}

export interface UpdateEventRequest {
  title?: string;
  titleEn?: string;
  description?: string;
  descriptionEn?: string;
  date?: string;
  location?: string;
  locationEn?: string;
  type?: string;
  zone?: string;
  capacity?: number;
  registrationDeadline?: string;
  meetingLink?: string;
  imageUrl?: string;
  status?: string;
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
  isActive?: boolean;
}

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
  status: 'Draft' | 'Sending' | 'Sent' | 'PartiallySent' | 'Failed';
  recipientCount: number;
  sentCount: number;
  failedCount: number;
  lastError?: string;
  createdAt: string;
  sentAt?: string;
}

export interface CreateNewsletterCampaignRequest {
  subject: string;
  subjectEn?: string;
  body: string;
  bodyEn?: string;
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
}

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
