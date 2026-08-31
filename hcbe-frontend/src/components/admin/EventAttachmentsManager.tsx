import React, { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { eventsApi } from '../../lib/api/events';
import type { EventAttachment } from '../../lib/api/types';
import { formatFileSize, resolveMediaUrl } from '../../lib/api/media-url';

interface EventAttachmentsManagerProps {
  eventId: string;
  attachments: EventAttachment[];
  onChange: (attachments: EventAttachment[]) => void;
}

export const EventAttachmentsManager: React.FC<EventAttachmentsManagerProps> = ({
  eventId,
  attachments,
  onChange,
}) => {
  const { t } = useTranslation();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState('');

  const handleUpload = async (files: FileList | null) => {
    if (!files?.length) return;

    setError('');
    setIsUploading(true);

    try {
      const uploaded: EventAttachment[] = [];
      for (const file of Array.from(files)) {
        const response = await eventsApi.uploadAttachment(eventId, file);
        if (response.success && response.data) {
          uploaded.push(response.data);
        } else {
          setError(response.message || t('admin.events.attachments.errorUpload'));
          break;
        }
      }

      if (uploaded.length > 0) {
        onChange([...attachments, ...uploaded]);
      }
    } catch (err) {
      console.error('Error uploading event attachment:', err);
      setError(t('admin.events.attachments.errorUpload'));
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleDelete = async (item: EventAttachment) => {
    if (!window.confirm(t('admin.events.attachments.confirmDelete'))) return;

    setError('');
    try {
      const response = await eventsApi.deleteAttachment(eventId, item.id);
      if (response.success) {
        onChange(attachments.filter((a) => a.id !== item.id));
      } else {
        setError(response.message || t('admin.events.attachments.errorDelete'));
      }
    } catch (err) {
      console.error('Error deleting event attachment:', err);
      setError(t('admin.events.attachments.errorDelete'));
    }
  };

  return (
    <div className="border border-line bg-surface p-6">
      <h2 className="mb-1 text-label-md uppercase text-ink-variant">
        {t('admin.events.attachments.title')}
      </h2>
      <p className="mb-4 text-body-md text-ink-variant">{t('admin.events.attachments.hint')}</p>

      {error && (
        <div className="mb-4 border border-error px-4 py-3 text-body-md text-error">
          {error}
        </div>
      )}

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <input
          ref={fileInputRef}
          type="file"
          accept=".pdf,.doc,.docx,.xls,.xlsx,image/jpeg,image/png,image/webp,image/gif"
          multiple
          className="hidden"
          onChange={(e) => handleUpload(e.target.files)}
        />
        <button
          type="button"
          disabled={isUploading}
          onClick={() => fileInputRef.current?.click()}
          className="inline-flex min-h-[44px] items-center gap-2 border-2 border-green px-6 py-3 text-label-md uppercase text-green transition-colors hover:bg-green hover:text-white disabled:opacity-50"
        >
          <i className="ri-attachment-2" aria-hidden="true" />
          {isUploading
            ? t('admin.events.attachments.uploading')
            : t('admin.events.attachments.add')}
        </button>
      </div>

      {attachments.length === 0 ? (
        <p className="text-body-md text-ink-variant">{t('admin.events.attachments.empty')}</p>
      ) : (
        <ul className="space-y-2">
          {attachments.map((item) => (
            <li
              key={item.id}
              className="flex items-center justify-between gap-3 border border-line px-3 py-2"
            >
              <a
                href={resolveMediaUrl(item.url)}
                target="_blank"
                rel="noopener noreferrer"
                className="min-w-0 flex-1 truncate text-body-md font-semibold text-green hover:underline"
              >
                <i className="ri-attachment-2 mr-2" aria-hidden="true" />
                {item.fileName}
                <span className="ml-2 text-ink-variant">({formatFileSize(item.sizeBytes)})</span>
              </a>
              <button
                type="button"
                onClick={() => handleDelete(item)}
                className="text-body-md font-semibold text-red-link hover:text-green"
              >
                {t('admin.events.attachments.delete')}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};
