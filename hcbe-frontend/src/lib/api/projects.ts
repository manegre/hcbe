import { apiClient } from './client';
import type { Project, CreateProjectRequest, UpdateProjectRequest } from './types';

export const projectsApi = {
  // Public methods
  getProjects: () => apiClient.get<Project[]>('/api/projects'),
  getProject: (id: string) => apiClient.get<Project>(`/api/projects/${id}`),

  // Admin methods  
  getProjectsForAdmin: () => apiClient.get<Project[]>('/api/projects/admin'),
  getProjectForAdmin: (id: string) => apiClient.get<Project>(`/api/projects/admin/${id}`),
  createProject: (data: CreateProjectRequest) => apiClient.post<Project>('/api/projects', data),
  updateProject: (id: string, data: UpdateProjectRequest) => apiClient.put<Project>(`/api/projects/${id}`, data),
  deleteProject: (id: string) => apiClient.delete(`/api/projects/${id}`),
  updateProjectProgress: (id: string, progress: number) => apiClient.put<Project>(`/api/projects/${id}/progress`, { progress }),
};
