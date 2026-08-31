import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { eventsApi } from '../../../../lib/api/events';
import type { CreateEventRequest } from '../../../../lib/api/types';
import { EventForm } from '../../../../components/forms/EventForm';

export const CreateEventPage: React.FC = () => {
  const { t } = useTranslation();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [pendingAttachments, setPendingAttachments] = useState<File[]>([]);
  const [coverFile, setCoverFile] = useState<File | null>(null);
  const navigate = useNavigate();

  const handleSubmit = async (data: CreateEventRequest) => {
    setIsLoading(true);
    setError('');

    try {
      let imageUrl = data.imageUrl || undefined;
      if (coverFile) {
        const mediaResponse = await eventsApi.uploadMedia(coverFile);
        if (!mediaResponse.success || !mediaResponse.data) {
          setError(mediaResponse.message || t('admin.events.form.errorUploadCover'));
          return;
        }
        imageUrl = mediaResponse.data.url;
      }

      const response = await eventsApi.createEvent({
        ...data,
        imageUrl,
      });

      if (!response.success || !response.data) {
        setError(response.message || t('admin.events.errorCreate'));
        return;
      }

      const eventId = response.data.id;
      for (const file of pendingAttachments) {
        const attachmentResponse = await eventsApi.uploadAttachment(eventId, file);
        if (!attachmentResponse.success) {
          setError(attachmentResponse.message || t('admin.events.attachments.errorUpload'));
          navigate(`/admin/events/${eventId}/edit`, {
            replace: true,
            state: { messageKey: 'admin.events.success.createdPartialMedia' },
          });
          return;
        }
      }

      navigate(`/admin/events/${eventId}/edit`, {
        replace: true,
        state: { messageKey: 'admin.events.success.createdAddMedia' },
      });
    } catch (err) {
      console.error('Error creating event:', err);
      setError(t('admin.common.errorUnexpected'));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-w-0">
      {error && (
        <div className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</div>
      )}

      <EventForm
        onSubmit={handleSubmit}
        isLoading={isLoading}
        submitButtonText={t('admin.events.form.submitCreate')}
        pendingAttachments={pendingAttachments}
        onPendingAttachmentsChange={setPendingAttachments}
        coverFile={coverFile}
        onCoverFileChange={setCoverFile}
      />
    </div>
  );
};
