import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { eventsApi } from '../../../../lib/api/events';
import type { UpdateEventRequest, Event } from '../../../../lib/api/types';
import { EventForm } from '../../../../components/forms/EventForm';
import { EventGalleryManager } from '../../../../components/admin/EventGalleryManager';
import { EventAttachmentsManager } from '../../../../components/admin/EventAttachmentsManager';

export const EditEventPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const { t } = useTranslation();
  const location = useLocation();
  const [event, setEvent] = useState<Event | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingEvent, setIsLoadingEvent] = useState(true);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [coverFile, setCoverFile] = useState<File | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const state = location.state as { messageKey?: string } | null;
    if (state?.messageKey) {
      setSuccessMessage(t(state.messageKey));
      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location, navigate, t]);

  useEffect(() => {
    if (id) {
      loadEvent(id);
    }
  }, [id]);

  const loadEvent = async (eventId: string) => {
    try {
      setIsLoadingEvent(true);
      const response = await eventsApi.getEventForAdmin(eventId);

      if (response.success && response.data) {
        setEvent(response.data);
      } else {
        setError(t('admin.events.errorNotFound'));
      }
    } catch (err) {
      console.error('Error loading event:', err);
      setError(t('admin.events.errorLoad'));
    } finally {
      setIsLoadingEvent(false);
    }
  };

  const handleSubmit = async (data: UpdateEventRequest) => {
    if (!id) return;

    setIsLoading(true);
    setError('');

    try {
      let imageUrl = data.imageUrl;
      if (coverFile) {
        const mediaResponse = await eventsApi.uploadMedia(coverFile);
        if (!mediaResponse.success || !mediaResponse.data) {
          setError(mediaResponse.message || t('admin.events.form.errorUploadCover'));
          return;
        }
        imageUrl = mediaResponse.data.url;
      }

      const response = await eventsApi.updateEvent(id, {
        ...data,
        imageUrl,
      });

      if (response.success && response.data) {
        navigate(`/admin/events/${id}`, {
          replace: true,
          state: { messageKey: 'admin.events.success.updated' },
        });
      } else {
        setError(response.message || t('admin.events.errorUpdate'));
      }
    } catch (err) {
      console.error('Error updating event:', err);
      setError(t('admin.common.errorUnexpected'));
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoadingEvent) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error && !event) {
    return (
      <div className="min-w-0">
        <div className="border border-error bg-surface px-6 py-12 text-center">
          <h3 className="font-display text-headline-md text-error">{t('admin.events.errorLoadTitle')}</h3>
          <p className="mt-2 text-body-md text-ink-variant">{error}</p>
          <button
            type="button"
            onClick={() => navigate('/admin/events')}
            className="mt-6 inline-flex min-h-[44px] items-center justify-center bg-gold px-6 py-3 text-label-md uppercase text-green hover:bg-gold-dim"
          >
            {t('admin.events.backToList')}
          </button>
        </div>
      </div>
    );
  }

  if (!event) {
    return null;
  }

  return (
    <div className="min-w-0">
      {successMessage && (
        <div className="mb-6 border border-green bg-surface px-4 py-3 text-green">{successMessage}</div>
      )}

      {error && (
        <div className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</div>
      )}

      <EventForm
        initialValues={event}
        onSubmit={handleSubmit}
        isLoading={isLoading}
        submitButtonText={t('admin.events.form.submitUpdate')}
        coverFile={coverFile}
        onCoverFileChange={setCoverFile}
        aside={
          <>
            <EventAttachmentsManager
              eventId={event.id}
              attachments={event.attachments ?? []}
              onChange={(attachments) => setEvent({ ...event, attachments })}
            />

            <EventGalleryManager
              eventId={event.id}
              media={event.media ?? []}
              onChange={(media) => setEvent({ ...event, media })}
            />
          </>
        }
      />
    </div>
  );
};
