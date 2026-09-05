import { apiClient } from './client';
import type { ApiResponse, ImpactDashboard } from './types';
export const impactApi = {
  get: (months = 6): Promise<ApiResponse<ImpactDashboard>> => apiClient.get<ImpactDashboard>(`/api/admin/impact?months=${months}`),
  exportCsv: (months = 6) => apiClient.download(`/api/admin/impact/export?months=${months}`),
  exportPdf: (months = 6) => apiClient.download(`/api/admin/impact/report.pdf?months=${months}`),
};
