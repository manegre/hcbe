import { apiClient } from './client';
import type { ApiResponse, ImpactDashboard } from './types';
export const impactApi = {
  get: (): Promise<ApiResponse<ImpactDashboard>> => apiClient.get<ImpactDashboard>('/api/admin/impact'),
  exportCsv: () => apiClient.download('/api/admin/impact/export'),
};
