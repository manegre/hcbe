import { getApiBaseUrl } from './base-url';
import type { ApiResponse } from './types';

export class ApiClient {
  private baseURL: string;

  constructor() {
    // In development, use relative URLs to leverage Vite proxy
    // In production or when VITE_API_URL is set, use the full URL
    this.baseURL = getApiBaseUrl();
  }

  private getAuthToken(): string | null {
    return localStorage.getItem('hcbe_token');
  }

  private getHeaders(includeAuth = true): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (includeAuth) {
      const token = this.getAuthToken();
      if (token) {
        headers.Authorization = `Bearer ${token}`;
      }
    }

    return headers;
  }

  private handleFetchError(error: unknown, endpoint: string): never {
    // Provide more detailed error information
    if (error instanceof TypeError && error.message === 'Failed to fetch') {
      throw new Error(`Unable to connect to API at ${this.baseURL}${endpoint}. Please ensure the backend server is running.`);
    }
    throw error;
  }

  private async fetchWithSession(
    endpoint: string,
    init: RequestInit,
    includeAuth = true,
  ): Promise<Response> {
    const execute = () => fetch(`${this.baseURL}${endpoint}`, {
      ...init,
      credentials: 'include',
      headers: this.getHeaders(includeAuth),
    });

    let response = await execute();
    const isAuthOperation = endpoint === '/api/auth/login' || endpoint === '/api/auth/refresh';
    if (response.status !== 401 || isAuthOperation || !includeAuth) {
      return response;
    }

    const refreshResponse = await fetch(`${this.baseURL}/api/auth/refresh`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
    });
    if (!refreshResponse.ok) return response;

    const refreshed = await refreshResponse.json() as ApiResponse<{ token: string; user: unknown }>;
    if (!refreshed.success || !refreshed.data?.token) return response;

    localStorage.setItem('hcbe_token', refreshed.data.token);
    localStorage.setItem('hcbe_user', JSON.stringify(refreshed.data.user));
    response = await execute();
    return response;
  }

  private async handleResponse<T>(response: Response): Promise<ApiResponse<T>> {
    // For login endpoint, 401 is expected with wrong credentials, so don't redirect
    const isLoginEndpoint =
      response.url.includes('/api/auth/login') ||
      response.url.includes('/api/auth/google/admin');
    
    if (response.status === 401 && !isLoginEndpoint) {
      // Unauthorized - clear token and redirect to login (but not for login endpoint itself)
      localStorage.removeItem('hcbe_token');
      localStorage.removeItem('hcbe_user');
      window.location.href = '/admin/login';
      throw new Error('Unauthorized');
    }

    if (!response.ok) {
      // Try to parse error response as JSON, fallback to text
      let errorMessage = `HTTP ${response.status}`;
      try {
        const errorText = await response.text();
        if (errorText) {
          try {
            const errorJson = JSON.parse(errorText);
            errorMessage = errorJson.message || errorText;
          } catch {
            errorMessage = errorText;
          }
        }
      } catch {
        // If we can't read the response, use status text
        errorMessage = response.statusText || `HTTP ${response.status}`;
      }
      
      // For 401 on login, return a proper error response
      if (response.status === 401 && isLoginEndpoint) {
        return {
          success: false,
          message: response.url.includes('/api/auth/google/admin')
            ? errorMessage
            : 'Invalid email or password',
          data: null,
          errors: null
        } as ApiResponse<T>;
      }
      
      throw new Error(errorMessage);
    }

    if (response.status === 204) {
      return {
        success: true,
        message: 'Success',
        data: null,
        errors: null,
      } as ApiResponse<T>;
    }

    const text = await response.text();
    if (!text.trim()) {
      return {
        success: true,
        message: 'Success',
        data: null,
        errors: null,
      } as ApiResponse<T>;
    }

    const data = JSON.parse(text);
    return data as ApiResponse<T>;
  }

  async get<T>(endpoint: string): Promise<ApiResponse<T>> {
    try {
      const response = await this.fetchWithSession(endpoint, {
        method: 'GET',
      });

      return this.handleResponse<T>(response);
    } catch (error) {
      this.handleFetchError(error, endpoint);
    }
  }

  async post<T>(endpoint: string, data?: any, includeAuth = true): Promise<ApiResponse<T>> {
    try {
      const response = await this.fetchWithSession(endpoint, {
        method: 'POST',
        body: data ? JSON.stringify(data) : undefined,
      }, includeAuth);

      return this.handleResponse<T>(response);
    } catch (error) {
      this.handleFetchError(error, endpoint);
    }
  }

  async put<T>(endpoint: string, data?: any): Promise<ApiResponse<T>> {
    try {
      const response = await this.fetchWithSession(endpoint, {
        method: 'PUT',
        body: data ? JSON.stringify(data) : undefined,
      });

      return this.handleResponse<T>(response);
    } catch (error) {
      this.handleFetchError(error, endpoint);
    }
  }

  async patch<T>(endpoint: string, data?: any): Promise<ApiResponse<T>> {
    try {
      const response = await this.fetchWithSession(endpoint, {
        method: 'PATCH',
        body: data ? JSON.stringify(data) : undefined,
      });

      return this.handleResponse<T>(response);
    } catch (error) {
      this.handleFetchError(error, endpoint);
    }
  }

  async delete<T>(endpoint: string): Promise<ApiResponse<T>> {
    try {
      const response = await this.fetchWithSession(endpoint, {
        method: 'DELETE',
      });

      return this.handleResponse<T>(response);
    } catch (error) {
      this.handleFetchError(error, endpoint);
    }
  }
}

export const apiClient = new ApiClient();
