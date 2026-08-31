export type VideoProvider = 'youtube' | 'vimeo';

export interface VideoEmbedInfo {
  provider: VideoProvider;
  embedUrl: string;
  watchUrl: string;
}

const YOUTUBE_HOSTS = new Set(['youtube.com', 'www.youtube.com', 'm.youtube.com', 'youtu.be']);
const VIMEO_HOSTS = new Set(['vimeo.com', 'www.vimeo.com', 'player.vimeo.com']);

const getYoutubeId = (url: URL): string | null => {
  if (url.hostname === 'youtu.be') {
    return url.pathname.replace('/', '').split('/')[0] || null;
  }

  if (url.pathname.startsWith('/embed/')) {
    return url.pathname.split('/')[2] || null;
  }

  if (url.pathname.startsWith('/shorts/')) {
    return url.pathname.split('/')[2] || null;
  }

  return url.searchParams.get('v');
};

const getVimeoId = (url: URL): string | null => {
  const parts = url.pathname.split('/').filter(Boolean);
  if (url.hostname === 'player.vimeo.com' && parts[0] === 'video') {
    return parts[1] || null;
  }
  return parts.find((part) => /^\d+$/.test(part)) || null;
};

export const getVideoEmbedInfo = (url?: string | null): VideoEmbedInfo | null => {
  if (!url) return null;

  try {
    const parsed = new URL(url);
    const host = parsed.hostname.toLowerCase();

    if (YOUTUBE_HOSTS.has(host)) {
      const id = getYoutubeId(parsed);
      if (!id) return null;
      return {
        provider: 'youtube',
        embedUrl: `https://www.youtube.com/embed/${id}`,
        watchUrl: url,
      };
    }

    if (VIMEO_HOSTS.has(host)) {
      const id = getVimeoId(parsed);
      if (!id) return null;
      return {
        provider: 'vimeo',
        embedUrl: `https://player.vimeo.com/video/${id}`,
        watchUrl: url,
      };
    }
  } catch {
    return null;
  }

  return null;
};
