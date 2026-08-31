import React, { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { EventMedia } from '../../lib/api/types';
import { resolveMediaUrl } from '../../lib/api/media-url';
import { getVideoEmbedInfo } from '../../lib/media/video-embed';
import { localizedOptional } from '../../lib/i18n/localized';

interface EventMediaGalleryProps {
  media: EventMedia[];
  title?: string;
}

export const EventMediaGallery: React.FC<EventMediaGalleryProps> = ({ media, title }) => {
  const { t, i18n } = useTranslation();
  const [activeIndex, setActiveIndex] = useState<number | null>(null);

  const sorted = useMemo(
    () => [...media].sort((a, b) => a.displayOrder - b.displayOrder),
    [media],
  );

  const canNavigate = sorted.length > 1;

  useEffect(() => {
    if (activeIndex === null) return;

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setActiveIndex(null);
        return;
      }
      if (!canNavigate) return;
      if (event.key === 'ArrowLeft') {
        event.preventDefault();
        setActiveIndex((index) =>
          index === null ? null : (index - 1 + sorted.length) % sorted.length,
        );
      }
      if (event.key === 'ArrowRight') {
        event.preventDefault();
        setActiveIndex((index) => (index === null ? null : (index + 1) % sorted.length));
      }
    };

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [activeIndex, canNavigate, sorted.length]);

  if (sorted.length === 0) return null;

  const active = activeIndex !== null ? sorted[activeIndex] : null;
  const activeVideo = active?.mediaType === 'video' ? getVideoEmbedInfo(active.url) : null;
  const activeCaption = active
    ? localizedOptional(active.caption, active.captionEn, i18n.language)
    : undefined;

  const goPrev = () => {
    setActiveIndex((index) =>
      index === null ? null : (index - 1 + sorted.length) % sorted.length,
    );
  };

  const goNext = () => {
    setActiveIndex((index) => (index === null ? null : (index + 1) % sorted.length));
  };

  return (
    <section className="mt-16 border-t border-line pt-10">
      <div className="mb-6 flex items-center gap-3">
        <span className="flex h-9 w-9 items-center justify-center rounded-full bg-gold text-green-deep">
          <i className="ri-gallery-line" aria-hidden="true" />
        </span>
        <h2 className="font-display text-headline-md text-green">
          {title || t('public.news.souvenirs.gallery.title')}
        </h2>
      </div>

      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        {sorted.map((item, index) => {
          const video = item.mediaType === 'video' ? getVideoEmbedInfo(item.url) : null;
          const caption = localizedOptional(item.caption, item.captionEn, i18n.language);
          return (
            <button
              key={item.id}
              type="button"
              onClick={() => setActiveIndex(index)}
              className="group overflow-hidden rounded-xl border border-green/10 bg-background text-left transition-all hover:-translate-y-0.5 hover:shadow-[0_12px_30px_rgba(0,59,27,.08)]"
            >
              {item.mediaType === 'image' ? (
                <img
                  src={resolveMediaUrl(item.url)}
                  alt={caption || ''}
                  className="h-48 w-full object-cover transition group-hover:scale-[1.02]"
                />
              ) : (
                <div className="flex h-48 items-center justify-center bg-ink text-white">
                  <div className="text-center">
                    <i className="ri-play-circle-line text-5xl" aria-hidden="true" />
                    <p className="mt-2 text-label-md uppercase">
                      {video?.provider === 'vimeo' ? 'Vimeo' : 'YouTube'}
                    </p>
                  </div>
                </div>
              )}
              {caption && (
                <p className="truncate px-4 py-3 text-body-md text-ink-variant">{caption}</p>
              )}
            </button>
          );
        })}
      </div>

      {active && activeIndex !== null && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-green-deep/95 p-4 backdrop-blur-sm"
          onClick={() => setActiveIndex(null)}
          role="dialog"
          aria-modal="true"
        >
          <button
            type="button"
            onClick={() => setActiveIndex(null)}
            className="absolute right-4 top-4 z-10 flex h-11 w-11 items-center justify-center rounded-full border border-white/30 bg-black/10 text-white transition-colors hover:border-gold hover:text-gold"
            aria-label={t('public.news.souvenirs.gallery.close')}
          >
            <i className="ri-close-line text-xl" aria-hidden="true" />
          </button>

          {canNavigate && (
            <>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  goPrev();
                }}
                className="absolute left-3 top-1/2 z-10 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full border border-white/30 bg-black/10 text-white transition-colors hover:border-gold hover:text-gold sm:left-6"
                aria-label={t('public.news.souvenirs.gallery.previous')}
              >
                <i className="ri-arrow-left-s-line text-2xl" aria-hidden="true" />
              </button>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  goNext();
                }}
                className="absolute right-3 top-1/2 z-10 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full border border-white/30 bg-black/10 text-white transition-colors hover:border-gold hover:text-gold sm:right-6"
                aria-label={t('public.news.souvenirs.gallery.next')}
              >
                <i className="ri-arrow-right-s-line text-2xl" aria-hidden="true" />
              </button>
            </>
          )}

          <div className="w-full max-w-5xl px-12 sm:px-16" onClick={(e) => e.stopPropagation()}>
            {active.mediaType === 'image' ? (
              <img
                src={resolveMediaUrl(active.url)}
                alt={activeCaption || ''}
                className="max-h-[80vh] w-full rounded-xl border border-white/20 object-contain shadow-2xl"
              />
            ) : activeVideo ? (
              <div className="aspect-video overflow-hidden border border-white/20 bg-ink">
                <iframe
                  key={active.id}
                  src={activeVideo.embedUrl}
                  title={activeCaption || t('public.news.souvenirs.gallery.video')}
                  className="h-full w-full"
                  allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                  allowFullScreen
                />
              </div>
            ) : (
              <a
                href={active.url}
                target="_blank"
                rel="noopener noreferrer"
                className="block border border-line bg-surface p-8 text-center font-semibold text-green"
              >
                {t('public.news.souvenirs.gallery.openExternal')}
              </a>
            )}
            <div className="mt-4 space-y-1 text-center">
              {activeCaption && <p className="text-body-md text-white/90">{activeCaption}</p>}
              {canNavigate && (
                <p className="text-label-md uppercase text-white">
                  {t('public.news.souvenirs.gallery.counter', {
                    current: activeIndex + 1,
                    total: sorted.length,
                  })}
                </p>
              )}
            </div>
          </div>
        </div>
      )}
    </section>
  );
};
