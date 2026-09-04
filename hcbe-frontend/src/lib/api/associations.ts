import { apiClient } from './client';
import { getApiBaseUrl } from './base-url';
import type {
  ApiResponse,
  Association,
  AssociationCalendarItem,
  AssociationCalendarMutation,
  AssociationClaim,
  AssociationDocument,
  AssociationJoinRequest,
  AssociationMember,
  AssociationMemberMutation,
  AssociationPermission,
  AssociationWorkspace,
  CreateAssociationRequest,
  MediaUpload,
  ServiceCase,
  UpdateAssociationRequest,
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

export const associationsApi = {
  getMyClaims: (): Promise<ApiResponse<AssociationClaim[]>> => apiClient.get<AssociationClaim[]>('/api/association-portal/claims/me'),
  claimAssociation: (id: string, message: string): Promise<ApiResponse<AssociationClaim>> => apiClient.post<AssociationClaim>(`/api/association-portal/${id}/claim`, { message }),
  getManagedAssociations: (): Promise<ApiResponse<Association[]>> => apiClient.get<Association[]>('/api/association-portal/managed'),
  getMyMembershipRequests: (): Promise<ApiResponse<AssociationJoinRequest[]>> => apiClient.get<AssociationJoinRequest[]>('/api/association-portal/memberships/me'),
  requestMembership: (id: string, message: string): Promise<ApiResponse<AssociationJoinRequest>> => apiClient.post<AssociationJoinRequest>(`/api/association-portal/${id}/join`, { message }),
  getWorkspace: (id: string): Promise<ApiResponse<AssociationWorkspace>> => apiClient.get<AssociationWorkspace>(`/api/association-portal/managed/${id}/workspace`),
  reviewMembership: (associationId: string, requestId: string, data: { status: 'Approved' | 'Rejected'; reviewNotes?: string; role?: AssociationMemberMutation['role']; title?: string; permissions?: AssociationPermission[] }): Promise<ApiResponse<AssociationJoinRequest>> => apiClient.put<AssociationJoinRequest>(`/api/association-portal/managed/${associationId}/requests/${requestId}`, data),
  updateWorkspaceMember: (associationId: string, associationMemberId: string, data: AssociationMemberMutation): Promise<ApiResponse<AssociationMember>> => apiClient.put<AssociationMember>(`/api/association-portal/managed/${associationId}/members/${associationMemberId}`, data),
  removeWorkspaceMember: (associationId: string, associationMemberId: string): Promise<ApiResponse<void>> => apiClient.delete<void>(`/api/association-portal/managed/${associationId}/members/${associationMemberId}`),
  addCalendarItem: (associationId: string, data: AssociationCalendarMutation): Promise<ApiResponse<AssociationCalendarItem>> => apiClient.post<AssociationCalendarItem>(`/api/association-portal/managed/${associationId}/calendar`, data),
  updateCalendarItem: (associationId: string, itemId: string, data: AssociationCalendarMutation): Promise<ApiResponse<AssociationCalendarItem>> => apiClient.put<AssociationCalendarItem>(`/api/association-portal/managed/${associationId}/calendar/${itemId}`, data),
  deleteCalendarItem: (associationId: string, itemId: string): Promise<ApiResponse<void>> => apiClient.delete<void>(`/api/association-portal/managed/${associationId}/calendar/${itemId}`),
  deleteWorkspaceDocument: (associationId: string, documentId: string): Promise<ApiResponse<void>> => apiClient.delete<void>(`/api/association-portal/managed/${associationId}/documents/${documentId}`),
  updateWorkspaceCase: (associationId: string, caseId: string, status: string): Promise<ApiResponse<ServiceCase>> => apiClient.patch<ServiceCase>(`/api/association-portal/managed/${associationId}/service-cases/${caseId}`, { status }),
  replyToWorkspaceCase: (associationId: string, caseId: string, body: string, isInternal = false): Promise<ApiResponse<ServiceCase>> => apiClient.post<ServiceCase>(`/api/association-portal/managed/${associationId}/service-cases/${caseId}/messages`, { body, isInternal }),
  updateManagedAssociation: (id: string, data: UpdateAssociationRequest): Promise<ApiResponse<Association>> => apiClient.put<Association>(`/api/association-portal/managed/${id}`, data),
  getClaimsForAdmin: (status?: string): Promise<ApiResponse<AssociationClaim[]>> => apiClient.get<AssociationClaim[]>(`/api/admin/association-claims${status ? `?status=${encodeURIComponent(status)}` : ''}`),
  reviewClaim: (id: string, status: 'Approved' | 'Rejected', adminNotes?: string): Promise<ApiResponse<AssociationClaim>> => apiClient.put<AssociationClaim>(`/api/admin/association-claims/${id}`, { status, adminNotes }),
  getAssociations: async (): Promise<ApiResponse<Association[]>> => {
    return await apiClient.get<Association[]>('/api/associations');
  },

  getAssociationsForAdmin: async (): Promise<ApiResponse<Association[]>> => {
    return await apiClient.get<Association[]>('/api/admin/associations');
  },

  getAssociation: async (id: string): Promise<ApiResponse<Association>> => {
    return await apiClient.get<Association>(`/api/associations/${id}`);
  },

  getAssociationForAdmin: async (id: string): Promise<ApiResponse<Association>> => {
    return await apiClient.get<Association>(`/api/admin/associations/${id}`);
  },

  createAssociation: async (data: CreateAssociationRequest): Promise<ApiResponse<Association>> => {
    return await apiClient.post<Association>('/api/associations', data);
  },

  updateAssociation: async (
    id: string,
    data: UpdateAssociationRequest,
  ): Promise<ApiResponse<Association>> => {
    return await apiClient.put<Association>(`/api/associations/${id}`, data);
  },

  deleteAssociation: async (id: string): Promise<ApiResponse<void>> => {
    return await apiClient.delete<void>(`/api/associations/${id}`);
  },

  uploadMedia: (file: File): Promise<ApiResponse<MediaUpload>> =>
    uploadMultipart<MediaUpload>('/api/media/upload', file, { folder: 'associations' }),

  uploadImage: (id: string, file: File): Promise<ApiResponse<MediaUpload>> =>
    uploadMultipart<MediaUpload>(`/api/associations/${id}/image`, file),

  uploadWorkspaceDocument: (associationId: string, file: File, metadata: { title: string; titleEn?: string; description?: string; descriptionEn?: string; visibility?: 'Members' | 'Managers' }): Promise<ApiResponse<AssociationDocument>> =>
    uploadMultipart<AssociationDocument>(`/api/association-portal/managed/${associationId}/documents`, file, {
      title: metadata.title,
      titleEn: metadata.titleEn ?? '',
      description: metadata.description ?? '',
      descriptionEn: metadata.descriptionEn ?? '',
      visibility: metadata.visibility ?? 'Members',
    }),

  getWorkspaceForAdmin: (associationId: string): Promise<ApiResponse<AssociationWorkspace>> => apiClient.get<AssociationWorkspace>(`/api/admin/association-workspaces/${associationId}`),
  reviewMembershipForAdmin: (associationId: string, requestId: string, data: { status: 'Approved' | 'Rejected'; reviewNotes?: string; role?: AssociationMemberMutation['role']; title?: string; permissions?: AssociationPermission[] }): Promise<ApiResponse<AssociationJoinRequest>> => apiClient.put<AssociationJoinRequest>(`/api/admin/association-workspaces/${associationId}/requests/${requestId}`, data),
  upsertMemberForAdmin: (associationId: string, data: AssociationMemberMutation & { memberId: string }): Promise<ApiResponse<AssociationMember>> => apiClient.put<AssociationMember>(`/api/admin/association-workspaces/${associationId}/members`, data),
  removeMemberForAdmin: (associationId: string, memberId: string): Promise<ApiResponse<void>> => apiClient.delete<void>(`/api/admin/association-workspaces/${associationId}/members/${memberId}`),
};
