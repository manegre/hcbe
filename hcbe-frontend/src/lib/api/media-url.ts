import { getApiBaseUrl } from './base-url';

/** Resolve relative upload paths against the API host; leave absolute URLs unchanged. */
export const resolveMediaUrl = (url?: string | null): string => {
  if (!url) return '';
  if (/^https?:\/\//i.test(url) || url.startsWith('blob:') || url.startsWith('data:')) {
    return url;
  }
  const path = url.startsWith('/') ? url : `/${url}`;
  return `${getApiBaseUrl()}${path}`;
};

export const formatFileSize = (sizeBytes: number): string => {
  if (sizeBytes >= 1048576) {
    return `${(sizeBytes / 1048576).toFixed(1)} MB`;
  }
  if (sizeBytes >= 1024) {
    return `${(sizeBytes / 1024).toFixed(1)} KB`;
  }
  return `${sizeBytes} B`;
};
