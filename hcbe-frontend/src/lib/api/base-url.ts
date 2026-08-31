export const getApiBaseUrl = (): string => {
  const configuredUrl = (import.meta.env.VITE_API_URL ?? '').trim();

  // In development, prefer same-origin + Vite proxy unless an explicit URL is set.
  if (import.meta.env.DEV && !configuredUrl) {
    return '';
  }

  return configuredUrl || 'http://localhost:8080';
};

export const buildApiUrl = (path: string): string => `${getApiBaseUrl()}${path}`;
