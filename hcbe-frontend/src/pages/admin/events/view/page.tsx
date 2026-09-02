import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { eventsApi } from '../../../../lib/api/events';
import type { Event } from '../../../../lib/api/types';
import { getEventLifecycle } from '../../../../lib/events/lifecycle';
import { getPublicationLabel, translateEventLifecycle } from '../../../../lib/i18n/adminStatus';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState } from '../../../../components/ui';
import { EventGalleryManager } from '../../../../components/admin/EventGalleryManager';
import { EventAttachmentsManager } from '../../../../components/admin/EventAttachmentsManager';
import { getEventCategoryLabel, useEventCategories } from '../../../../lib/events/categories';
import { formatEventDateTime } from '../../../../lib/events/timezone';

const eventLifecycleChipStatus = (event: Event): 'published' | 'draft' | 'past' | 'rejected' => {
  const lifecycle = getEventLifecycle(event);
  if (lifecycle === 'past') return 'past';
  if (lifecycle === 'draft') return 'draft';
  if (lifecycle === 'cancelled') return 'rejected';
  return 'published';
};

export const ViewEventPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const [event, setEvent] = useState<Event | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const navigate = useNavigate();
  const location = useLocation();
  const categories = useEventCategories(true);

  useEffect(() => {
    if (id) {
      loadEvent(id);
    }
  }, [id]);

  useEffect(() => {
    const messageKey = (location.state as { messageKey?: string } | null)?.messageKey;
    if (!messageKey) return;

    setSuccessMessage(t(messageKey));
    navigate(location.pathname, { replace: true, state: {} });
  }, [location.state, location.pathname, navigate, t]);

  const loadEvent = async (eventId: string) => {
    try {
      setIsLoading(true);
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
      setIsLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!event || !id) return;

    if (!window.confirm(t('admin.events.confirmDelete', { title: event.title }))) {
      return;
    }

    try {
      const response = await eventsApi.deleteEvent(id);
      if (response.success) {
        navigate('/admin/events', {
          state: { messageKey: 'admin.events.success.deleted' },
        });
      } else {
        setError(t('admin.events.errorDelete'));
      }
    } catch (err) {
      console.error('Error deleting event:', err);
      setError(t('admin.events.errorDelete'));
    }
  };

  const locale = i18n.language.startsWith('fr') ? 'fr-CA' : 'en-CA';

  const formatDate = (dateString: string, timeZone?: string) =>
    formatEventDateTime(dateString, locale, timeZone, {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      timeZoneName: 'short',
    });

  const formatShortDate = (dateString: string) =>
    new Intl.DateTimeFormat(locale, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    }).format(new Date(dateString));

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error && !event) {
    return (
      <EmptyState
        tone="error"
        title={t('admin.events.errorLoadTitle')}
        description={error}
        action={
          <Button to="/admin/events" variant="secondary">
            {t('admin.events.backToList')}
          </Button>
        }
      />
    );
  }

  if (!event) {
    return null;
  }

  const speakers = event.speakers ?? [];
  const organizers = event.organizers ?? [];
  const categoryLabel = getEventCategoryLabel(event.type, categories, i18n.language);

  return (
    <AdminDetailLayout
      title={event.title}
      backPath="/admin/events"
      backLabel={t('admin.events.backToList')}
      status={{ status: eventLifecycleChipStatus(event), label: translateEventLifecycle(event, t) }}
      subtitle={getPublicationLabel(event.status, t)}
      actions={
        <>
          <Button to={`/admin/events/${id}/edit`} variant="secondary">
            {t('admin.events.edit')}
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            {t('admin.events.delete')}
          </Button>
        </>
      }
      main={
        <>
          {successMessage && (
            <div className="border border-green bg-surface px-4 py-3 text-green">{successMessage}</div>
          )}

          {error && <div className="border border-error bg-surface px-4 py-3 text-error">{error}</div>}

          {event.imageUrl && (
            <img
              src={event.imageUrl}
              alt={event.title}
              className="h-64 w-full border border-line object-cover"
              onError={(e) => {
                e.currentTarget.style.display = 'none';
              }}
            />
          )}

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.events.detailsSection')}</h2>
            <DetailList>
              <DetailRow label={t('admin.events.form.startDate')} value={formatDate(event.date, event.timeZone)} />
              {event.endDate && <DetailRow label={t('admin.events.form.endDate')} value={formatDate(event.endDate, event.timeZone)} />}
              <DetailRow label={t('admin.events.form.timeZone')} value={event.timeZone || 'America/Toronto'} />
              {event.location && <DetailRow label={t('admin.common.location')} value={event.location} />}
              {categoryLabel && <DetailRow label={t('admin.common.type')} value={categoryLabel} />}
              <DetailRow label={t('admin.events.form.formatSection')} value={t(`admin.events.format.${event.format || 'InPerson'}`)} />
              {event.zone && <DetailRow label={t('admin.common.zone')} value={event.zone} />}
              {speakers.length > 0 && (
                <DetailRow label={t('admin.events.speakers')} value={speakers.join(', ')} />
              )}
              {organizers.length > 0 && (
                <DetailRow label={t('admin.events.form.organizers')} value={organizers.join(', ')} />
              )}
              {event.capacity && (
                <DetailRow
                  label={t('admin.common.capacity')}
                  value={t('admin.events.attendees', { count: event.capacity })}
                />
              )}
            </DetailList>
          </div>

          {event.meetingLink && (
            <div>
              <h2 className="font-display text-headline-sm text-green">{t('admin.events.meetingLink')}</h2>
              <a
                href={event.meetingLink}
                target="_blank"
                rel="noopener noreferrer"
                className="mt-2 inline-block break-all text-body-md text-red-link hover:text-green"
              >
                {event.meetingLink}
              </a>
            </div>
          )}

          {event.registrationUrl && (
            <div>
              <h2 className="font-display text-headline-sm text-green">{t('admin.events.form.registrationUrl')}</h2>
              <a href={event.registrationUrl} target="_blank" rel="noopener noreferrer" className="mt-2 inline-block break-all text-body-md text-red-link hover:text-green">
                {event.ctaLabel || event.registrationUrl}
              </a>
            </div>
          )}

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.events.additionalSection')}</h2>
            <DetailList>
              {event.registrationDeadline && (
                <DetailRow
                  label={t('admin.events.registrationDeadline')}
                  value={formatDate(event.registrationDeadline, event.timeZone)}
                />
              )}
              <DetailRow label={t('admin.events.createdAt')} value={formatShortDate(event.createdAt)} />
              <DetailRow label={t('admin.events.updatedAt')} value={formatShortDate(event.updatedAt)} />
            </DetailList>
          </div>

          {event.description && (
            <div>
              <h2 className="font-display text-headline-sm text-green">{t('admin.common.description')}</h2>
              <p className="mt-3 whitespace-pre-wrap text-body-md text-ink-variant">{event.description}</p>
            </div>
          )}

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
  );
};
