import { apiClient } from './client';
import type { AccountSession, AuditLog, MfaConfirmation, MfaEnrollment, MfaStatus, SecurityIncident, SecurityPosture } from './types';

export const securityApi = {
  getMfaStatus: () => apiClient.get<MfaStatus>('/api/security/mfa'),
  beginMfaEnrollment: () => apiClient.post<MfaEnrollment>('/api/security/mfa/enroll'),
  confirmMfaEnrollment: (code: string) => apiClient.post<MfaConfirmation>('/api/security/mfa/confirm', { code }),
  disableMfa: (code: string) => apiClient.post<MfaStatus>('/api/security/mfa/disable', { code }),
  getSessions: () => apiClient.get<AccountSession[]>('/api/security/sessions'),
  revokeSession: (id: string) => apiClient.delete<boolean>(`/api/security/sessions/${id}`),
  revokeOtherSessions: () => apiClient.post<number>('/api/security/sessions/revoke-others'),
  getPosture: () => apiClient.get<SecurityPosture>('/api/admin/security/posture'),
  getIncidents: (includeResolved = false) => apiClient.get<SecurityIncident[]>(`/api/admin/security/incidents?includeResolved=${includeResolved}`),
  createIncident: (data: Partial<SecurityIncident>) => apiClient.post<SecurityIncident>('/api/admin/security/incidents', data),
  updateIncident: (id: string, data: Partial<SecurityIncident>) => apiClient.put<SecurityIncident>(`/api/admin/security/incidents/${id}`, data),
  getAudit: () => apiClient.get<{ items: AuditLog[]; total: number }>('/api/admin/security/audit?pageSize=40'),
  reviewAccess: (userId: string, decision: 'Retain' | 'Modify' | 'Remove', notes?: string) => apiClient.post(`/api/admin/security/access-reviews/${userId}`, { decision, notes }),
};
