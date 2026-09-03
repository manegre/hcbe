import { apiClient } from './client';
import type { ApiResponse } from './types';

export interface ErrorIncident {
  id: string;
  traceId: string;
  httpMethod: string;
  path: string;
  exceptionType: string;
  message: string;
  occurrenceCount: number;
  firstOccurredAtUtc: string;
  lastOccurredAtUtc: string;
  resolvedAtUtc?: string | null;
}

export const errorIncidentsApi = {
  list: (includeResolved = false): Promise<ApiResponse<ErrorIncident[]>> =>
    apiClient.get<ErrorIncident[]>(`/api/admin/error-incidents?includeResolved=${includeResolved}`),
  resolve: (id: string): Promise<ApiResponse<void>> =>
    apiClient.put<void>(`/api/admin/error-incidents/${id}/resolve`),
};
