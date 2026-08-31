import React, { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { eventsApi } from '../../lib/api/events';
import type { EventMedia } from '../../lib/api/types';
import { resolveMediaUrl } from '../../lib/api/media-url';
import { getVideoEmbedInfo } from '../../lib/media/video-embed';
import { Field, inputClasses } from '../ui';

interface EventGalleryManagerProps {
  eventId: string;
  media: EventMedia[];
  onChange: (media: EventMedia[]) => void;
}

export const EventGalleryManager: React.FC<EventGalleryManagerProps> = ({
  eventId,
  media,
  onChange,
}) => {
  const { t } = useTranslation();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [videoUrl, setVideoUrl] = useState('');
  const [videoCaption, setVideoCaption] = useState('');
  const [videoCaptionEn, setVideoCaptionEn] = useState('');
  const [isUploading, setIsUploading] = useState(false);
  const [isAddingVideo, setIsAddingVideo] = useState(false);
  const [error, setError] = useState('');

  const sortedMedia = [...media].sort((a, b) => a.displayOrder - b.displayOrder);

  const handlePhotoUpload = async (files: FileList | null) => {
    if (!files?.length) return;

    setError('');
    setIsUploading(true);

    try {
      const uploaded: EventMedia[] = [];
      for (const file of Array.from(files)) {
        const response = await eventsApi.uploadPhoto(eventId, file);
        if (response.success && response.data) {
          uploaded.push(response.data);
        } else {
          setError(response.message || t('admin.events.gallery.errorUpload'));
          break;
        }
      }

      if (uploaded.length > 0) {
        onChange([...media, ...uploaded]);
      }
    } catch (err) {
      console.error('Error uploading event photo:', err);
      setError(t('admin.events.gallery.errorUpload'));
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleAddVideo = async (event: React.FormEvent) => {
    event.preventDefault();
    const trimmed = videoUrl.trim();
    if (!trimmed) return;

    if (!getVideoEmbedInfo(trimmed)) {
      setError(t('admin.events.gallery.errorVideoUrl'));
      return;
    }

    setError('');
    setIsAddingVideo(true);

    try {
      const response = await eventsApi.addVideo(
        eventId,
        trimmed,
        videoCaption.trim() || undefined,
        videoCaptionEn.trim() || undefined,
      );
      if (response.success && response.data) {
        onChange([...media, response.data]);
        setVideoUrl('');
        setVideoCaption('');
        setVideoCaptionEn('');
      } else {
        setError(response.message || t('admin.events.gallery.errorVideo'));
      }
    } catch (err) {
      console.error('Error adding event video:', err);
      setError(t('admin.events.gallery.errorVideo'));
    } finally {
      setIsAddingVideo(false);
    }
  };

  const handleDelete = async (item: EventMedia) => {
    if (!window.confirm(t('admin.events.gallery.confirmDelete'))) return;

    setError('');
    try {
      const response = await eventsApi.deleteMedia(eventId, item.id);
      if (response.success) {
        onChange(media.filter((m) => m.id !== item.id));
      } else {
        setError(response.message || t('admin.events.gallery.errorDelete'));
      }
    } catch (err) {
      console.error('Error deleting event media:', err);
      setError(t('admin.events.gallery.errorDelete'));
    }
  };

  return (
    <div className="border border-line bg-surface p-6">
      <h2 className="mb-1 text-label-md uppercase text-ink-variant">{t('admin.events.gallery.title')}</h2>
      <p className="mb-4 text-body-md text-ink-variant">{t('admin.events.gallery.hint')}</p>

      {error && (
        <div className="mb-4 border border-error px-4 py-3 text-body-md text-error">
          {error}
        </div>
      )}

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <input
          ref={fileInputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp,image/gif"
          multiple
          className="hidden"
          onChange={(e) => handlePhotoUpload(e.target.files)}
        />
        <button
          type="button"
          disabled={isUploading}
          onClick={() => fileInputRef.current?.click()}
          className="inline-flex min-h-[44px] items-center gap-2 border-2 border-green px-6 py-3 text-label-md uppercase text-green transition-colors hover:bg-green hover:text-white disabled:opacity-50"
        >
          <i className="ri-image-add-line" aria-hidden="true" />
          {isUploading ? t('admin.events.gallery.uploading') : t('admin.events.gallery.addPhotos')}
        </button>
      </div>

      <form onSubmit={handleAddVideo} className="mb-6 flex flex-col gap-3">
        <Field label={t('admin.events.gallery.videoUrlPlaceholder')} htmlFor="gallery-video-url">
          <input
            type="url"
            id="gallery-video-url"
            value={videoUrl}
            onChange={(e) => setVideoUrl(e.target.value)}
            className={inputClasses}
          />
        </Field>
        <Field label={t('admin.events.gallery.captionPlaceholder')} htmlFor="gallery-video-caption">
          <input
            type="text"
            id="gallery-video-caption"
            value={videoCaption}
            onChange={(e) => setVideoCaption(e.target.value)}
            className={inputClasses}
          />
        </Field>
        <Field label={t('admin.events.gallery.captionEnPlaceholder')} htmlFor="gallery-video-caption-en">
          <input
            type="text"
            id="gallery-video-caption-en"
            value={videoCaptionEn}
            onChange={(e) => setVideoCaptionEn(e.target.value)}
            className={inputClasses}
          />
        </Field>
        <button
          type="submit"
          disabled={isAddingVideo || !videoUrl.trim()}
          className="inline-flex min-h-[44px] items-center justify-center gap-2 rounded-control bg-gold px-6 py-3 text-label-md uppercase text-green transition-colors duration-200 hover:bg-gold-dim focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-green disabled:opacity-50"
        >
          <i className="ri-video-add-line" aria-hidden="true" />
          {isAddingVideo ? t('admin.events.gallery.adding') : t('admin.events.gallery.addVideo')}
        </button>
      </form>

      {sortedMedia.length === 0 ? (
        <p className="text-body-md text-ink-variant">{t('admin.events.gallery.empty')}</p>
      ) : (
        <ul className="grid grid-cols-2 gap-3">
          {sortedMedia.map((item) => {
            const video = item.mediaType === 'video' ? getVideoEmbedInfo(item.url) : null;
            return (
              <li key={item.id} className="border border-line bg-surface-container">
                {item.mediaType === 'image' ? (
                  <img
                    src={resolveMediaUrl(item.url)}
                    alt={item.caption || item.fileName || ''}
                    className="aspect-square w-full object-cover"
                  />
                ) : (
                  <div className="flex aspect-square items-center justify-center bg-green text-white">
                    <div className="px-2 text-center">
                      <i className="ri-youtube-line text-3xl" aria-hidden="true" />
                      <p className="mt-2 truncate text-body-md text-green-dim">
                        {video?.provider === 'vimeo' ? 'Vimeo' : 'YouTube'}
                      </p>
                    </div>
                  </div>
                )}
                <div className="space-y-2 p-3">
                  {item.caption && <p className="text-body-md text-ink">{item.caption}</p>}
                  {item.mediaType === 'video' && (
                    <a
                      href={item.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="block truncate text-body-md text-green hover:underline"
                    >
                      {item.url}
                    </a>
                  )}
                  <button
                    type="button"
                    onClick={() => handleDelete(item)}
                    className="text-body-md font-semibold text-red-link hover:text-green"
                  >
                    {t('admin.events.gallery.delete')}
                  </button>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
};
