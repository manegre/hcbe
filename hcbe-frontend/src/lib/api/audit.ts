import { apiClient } from './client';
import type { AuditLogPage } from './types';

export interface AuditLogQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  action?: string;
  entityType?: string;
  userEmail?: string;
  fromUtc?: string;
  toUtc?: string;
}

const toQueryString = (query: AuditLogQuery) => {
  const params = new URLSearchParams();
  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined && value !== '') params.set(key, String(value));
  });
  return params.toString();
};

export const auditApi = {
  list: (query: AuditLogQuery = {}) =>
    apiClient.get<AuditLogPage>(`/api/admin/audit-logs?${toQueryString(query)}`),
};
