import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import Navbar from '../../../../components/feature/Navbar';
import Footer from '../../../../components/feature/Footer';
import { buildApiUrl } from '../../../../lib/api/base-url';
import { resolveMediaUrl } from '../../../../lib/api/media-url';
import { getEventLifecycle } from '../../../../lib/events/lifecycle';
import { EventMediaGallery } from '../../../../components/events/EventMediaGallery';
import type { Event } from '../../../../lib/api/types';
import { localized, localizedOptional } from '../../../../lib/i18n/localized';
import { formatFileSize } from '../../../../lib/api/media-url';
import { isImageFile } from '../../../../lib/media/is-image-file';
import ImageCarousel from '../../../../components/media/ImageCarousel';
import { ArrowLink, Button, EmptyState, StatusChip, Tag } from '../../../../components/ui';
import { getEventCategoryLabel, useEventCategories } from '../../../../lib/events/categories';
import { formatEventDateTime } from '../../../../lib/events/timezone';

export const EventDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const [event, setEvent] = useState<Event | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const categories = useEventCategories();

  useEffect(() => {
    if (id) {
      loadEvent(id);
    }
  }, [id]);

  const loadEvent = async (eventId: string) => {
    try {
      const response = await fetch(buildApiUrl(`/api/events/${eventId}`));
      const result = await response.json();
      if (result.success) {
        setEvent(result.data);
      } else {
        setTimeout(() => navigate('/actualites/evenements'), 2000);
      }
    } catch (error) {
      console.error('Error loading event:', error);
      setTimeout(() => navigate('/actualites/evenements'), 2000);
    } finally {
      setIsLoading(false);
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

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background">
        <Navbar />
        <div className="container-page py-16">
          <div className="space-y-4">
            <div className="h-4 w-40 animate-pulse bg-surface-container" />
            <div className="h-10 w-2/3 animate-pulse bg-surface-container" />
            <div className="h-64 w-full animate-pulse bg-surface-container" />
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  if (!event) {
    return (
      <div className="min-h-screen bg-background">
        <Navbar />
        <div className="container-page py-16">
          <EmptyState
            icon="ri-calendar-line"
            title={t('public.news.evenements.empty.title')}
            action={
              <ArrowLink to="/actualites/evenements" tone="red">
                {t('public.news.evenements.backToList')}
              </ArrowLink>
            }
          />
        </div>
        <Footer />
      </div>
    );
  }

  const lifecycle = getEventLifecycle(event);
  const isPast = lifecycle === 'past';
  const isVirtual =
    event.format === 'Online' || event.format === 'Hybrid' || Boolean(event.meetingLink);

  const attachments = event.attachments ?? [];
  const speakers = event.speakers ?? [];
  const organizers = event.organizers ?? [];
  const imageAttachments = attachments.filter((attachment) =>
    isImageFile(attachment.contentType, attachment.fileName),
  );
  const fileAttachments = attachments.filter(
    (attachment) => !isImageFile(attachment.contentType, attachment.fileName),
  );

  const location = localizedOptional(event.location, event.locationEn, i18n.language);
  const typeLabel = getEventCategoryLabel(event.type, categories, i18n.language);

  const statusForChip =
    lifecycle === 'ongoing' ? 'approved' : lifecycle === 'upcoming' ? 'pending' : lifecycle === 'past' ? 'past' : null;
  const statusLabel =
    lifecycle === 'ongoing'
      ? t('public.news.evenements.status.ongoing')
      : lifecycle === 'upcoming'
        ? t('public.news.evenements.status.upcoming')
        : lifecycle === 'past'
          ? t('public.news.evenements.status.past')
          : '';

  const registerHref = event.registrationUrl || event.meetingLink;
  const actionLabel = localizedOptional(event.ctaLabel, event.ctaLabelEn, i18n.language) ||
    (event.registrationUrl
      ? t('public.news.evenements.cta.register')
      : event.meetingLink
        ? t('public.news.evenements.joinMeeting')
        : t('public.news.evenements.cta.register'));
  const internalRegisterHref = `/contact?type=event-registration&referenceId=${encodeURIComponent(event.id)}&label=${encodeURIComponent(localized(event.title, event.titleEn, i18n.language))}`;

  const practicalDetails = [
    { icon: 'ri-calendar-line', value: formatDate(event.date, event.timeZone) },
    event.endDate
      ? { icon: 'ri-time-line', value: `${t('public.news.evenements.endsAt')} · ${formatDate(event.endDate, event.timeZone)}` }
      : null,
    location ? { icon: 'ri-map-pin-line', value: location } : null,
    event.format
      ? { icon: event.format === 'Online' ? 'ri-live-line' : event.format === 'Hybrid' ? 'ri-links-line' : 'ri-map-pin-2-line', value: t(`public.news.evenements.format.${event.format}`) }
      : null,
    typeLabel ? { icon: 'ri-bookmark-line', value: typeLabel } : null,
    event.capacity
      ? { icon: 'ri-group-line', value: t('admin.events.attendees', { count: event.capacity }) }
      : null,
    event.registrationDeadline
      ? {
          icon: 'ri-calendar-check-line',
          value: `${t('public.news.evenements.registrationDeadline')} ${formatDate(event.registrationDeadline, event.timeZone)}`,
        }
      : null,
  ].filter((item): item is { icon: string; value: string } => item !== null);

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <header className="public-grid-pattern relative overflow-hidden bg-green-deep py-12 md:py-16">
        <div className="pointer-events-none absolute -right-20 -top-24 h-72 w-72 rounded-full border-[48px] border-white/5" aria-hidden="true" />
        <div className="container-page">
          <Link
            to="/actualites/evenements"
            className="inline-flex min-h-[44px] items-center gap-2 text-[10px] font-bold uppercase tracking-[0.14em] text-white/70 transition-colors hover:text-gold"
          >
            <i className="ri-arrow-left-line" aria-hidden="true"></i>
            {t('public.news.evenements.backToList')}
          </Link>

          <div className="mt-6 flex flex-wrap items-center gap-4">
            {statusForChip && <StatusChip status={statusForChip} label={statusLabel} />}
            {isVirtual && (
              <Tag>
                <i className={`${event.format === 'Hybrid' ? 'ri-links-line' : 'ri-video-line'} mr-1`} aria-hidden="true"></i>
                {t(`public.news.evenements.format.${event.format || 'Online'}`)}
              </Tag>
            )}
          </div>

          <h1 className="mt-5 max-w-4xl font-display text-[36px] font-bold leading-[1.06] tracking-[-0.03em] text-white md:text-[56px]">
            {localized(event.title, event.titleEn, i18n.language)}
          </h1>

          <dl className="mt-8 grid grid-cols-1 divide-y divide-white/15 border-y border-white/15 sm:grid-cols-4 sm:divide-x sm:divide-y-0">
            <div className="py-4 sm:pr-6">
              <dt className="text-[10px] font-bold uppercase tracking-[0.12em] text-gold">{t('public.news.evenements.startsAt')}</dt>
              <dd className="mt-2 text-body-md text-white/80">{formatDate(event.date, event.timeZone)}</dd>
            </div>
            {event.endDate && (
              <div className="py-4 sm:px-6">
                <dt className="text-[10px] font-bold uppercase tracking-[0.12em] text-gold">{t('public.news.evenements.endsAt')}</dt>
                <dd className="mt-2 text-body-md text-white/80">{formatDate(event.endDate, event.timeZone)}</dd>
              </div>
            )}
            {location && (
              <div className="py-4 sm:px-6">
                <dt className="text-[10px] font-bold uppercase tracking-[0.12em] text-gold">{t('admin.common.location')}</dt>
                <dd className="mt-2 text-body-md text-white/80">{location}</dd>
              </div>
            )}
            {typeLabel && (
              <div className="py-4 sm:px-6">
                <dt className="text-[10px] font-bold uppercase tracking-[0.12em] text-gold">{t('admin.common.type')}</dt>
                <dd className="mt-2 text-body-md text-white/80">{typeLabel}</dd>
              </div>
            )}
            {event.capacity && (
              <div className="py-4 sm:pl-6">
                <dt className="text-[10px] font-bold uppercase tracking-[0.12em] text-gold">{t('admin.common.capacity')}</dt>
                <dd className="mt-2 text-body-md text-white/80">
                  {t('admin.events.attendees', { count: event.capacity })}
                </dd>
              </div>
            )}
          </dl>

          {isPast && (
            <div className="mt-8 rounded-r-lg border-l-2 border-gold bg-white/10 p-4 text-body-md text-white/75">
              {t('public.news.evenements.pastNotice')}
            </div>
          )}
        </div>
      </header>

      <main className="bg-surface-container py-12 md:py-16">
        <div className="container-page">
          {event.imageUrl && (
            <div className="relative flex h-[360px] w-full items-center justify-center overflow-hidden rounded-[20px] bg-green-deep text-gold shadow-[0_22px_55px_rgba(0,59,27,.14)] md:h-[460px]">
              <i className="ri-calendar-event-line text-5xl" aria-hidden="true" />
              <img
                src={resolveMediaUrl(event.imageUrl)}
                alt=""
                className={`absolute inset-0 h-full w-full object-cover ${isPast ? 'grayscale-[30%]' : ''}`}
                onError={(imageEvent) => {
                  imageEvent.currentTarget.style.display = 'none';
                }}
              />
            </div>
          )}

          <div className="mt-8 grid grid-cols-1 gap-8 lg:grid-cols-3">
            <div className="rounded-[20px] border border-green/10 bg-white p-6 shadow-[0_18px_48px_rgba(0,59,27,.07)] md:p-10 lg:col-span-2">
              {speakers.length > 0 && (
                <section className="mb-8 border-b border-line pb-8">
                  <p className="flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.14em] text-red-link">
                    <i className="ri-user-voice-line text-base" aria-hidden="true" />
                    {t('public.news.evenements.speakers')}
                  </p>
                  <ul className="mt-4 flex flex-wrap gap-2">
                    {speakers.map((speaker) => (
                      <li
                        key={speaker}
                        className="rounded-full border border-green/15 bg-surface-container px-4 py-2 text-body-md font-medium text-green"
                      >
                        {speaker}
                      </li>
                    ))}
                  </ul>
                </section>
              )}

              {organizers.length > 0 && (
                <section className="mb-8 border-b border-line pb-8">
                  <p className="flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.14em] text-red-link">
                    <i className="ri-community-line text-base" aria-hidden="true" />
                    {t('public.news.evenements.organizers')}
                  </p>
                  <ul className="mt-4 flex flex-wrap gap-x-6 gap-y-2">
                    {organizers.map((organizer) => (
                      <li key={organizer} className="flex items-center gap-2 text-body-md font-medium text-green">
                        <span className="h-1.5 w-1.5 rounded-full bg-gold" aria-hidden="true" />
                        {organizer}
                      </li>
                    ))}
                  </ul>
                </section>
              )}

              <div className="max-w-[65ch] whitespace-pre-line text-[17px] leading-8 text-ink-variant">
                {localized(event.description, event.descriptionEn, i18n.language)}
              </div>

              {(event.media?.length ?? 0) > 0 && (
                <EventMediaGallery media={event.media ?? []} />
              )}

              {(imageAttachments.length > 0 || fileAttachments.length > 0) && (
                <section className="mt-16">
                  <h2 className="border-b border-line pb-4 font-display text-headline-md text-green">
                    {t('public.news.evenements.attachments')}
                  </h2>

                  {imageAttachments.length > 0 && (
                    <div className="mt-8">
                      <ImageCarousel
                        images={imageAttachments.map((attachment) => ({
                          id: attachment.id,
                          url: attachment.url,
                          alt: attachment.fileName,
                        }))}
                      />
                    </div>
                  )}

                  {fileAttachments.length > 0 && (
                    <ul className="mt-8 divide-y divide-line overflow-hidden rounded-xl border border-line bg-background px-5">
                      {fileAttachments.map((attachment) => (
                        <li key={attachment.id} className="flex flex-wrap items-center justify-between gap-4 py-4">
                          <div>
                            <p className="text-body-md text-ink">{attachment.fileName}</p>
                            <p className="mt-1 text-label-md uppercase text-ink-variant">
                              {(attachment.contentType || '').split('/').pop() || 'FICHIER'} ·{' '}
                              {formatFileSize(attachment.sizeBytes)}
                            </p>
                          </div>
                          <a
                            href={resolveMediaUrl(attachment.url)}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase text-red-link transition-colors hover:text-green"
                          >
                            {t('public.services.documents.download')}
                            <i className="ri-arrow-right-line" aria-hidden="true"></i>
                          </a>
                        </li>
                      ))}
                    </ul>
                  )}
                </section>
              )}
            </div>

            <aside className="lg:sticky lg:top-24 lg:h-fit">
              <div className="rounded-[18px] border border-green/10 bg-white p-6 shadow-[0_14px_35px_rgba(0,59,27,.07)]">
                <h2 className="font-display text-headline-md text-green">
                  {t('public.news.evenements.cta.details')}
                </h2>
                <dl className="mt-6 space-y-4">
                  {practicalDetails.map((detail) => (
                    <div key={detail.icon} className="flex items-start gap-3">
                      <i className={`${detail.icon} mt-0.5 text-gold-ink`} aria-hidden="true"></i>
                      <dd className="text-body-md text-ink">{detail.value}</dd>
                    </div>
                  ))}
                </dl>

                {!isPast &&
                  (registerHref ? (
                    <a
                      href={registerHref}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="mt-6 inline-flex min-h-[44px] w-full items-center justify-center gap-2 rounded-control border border-transparent bg-gold px-6 py-3 text-label-md uppercase text-green transition-colors hover:bg-gold-dim focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-green"
                    >
                      {actionLabel}
                    </a>
                  ) : (
                    <Button to={internalRegisterHref} variant="primary" className="mt-6 w-full">
                      {actionLabel}
                    </Button>
                  ))}
              </div>
            </aside>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
};

export default EventDetailPage;
