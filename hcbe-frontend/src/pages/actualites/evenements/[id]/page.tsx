import React, { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import Navbar from '../../../../components/feature/Navbar';
import Footer from '../../../../components/feature/Footer';
import { EventMediaGallery } from '../../../../components/events/EventMediaGallery';
import ImageCarousel from '../../../../components/media/ImageCarousel';
import { ArrowLink, EmptyState, RichTextContent, StatusChip } from '../../../../components/ui';
import { buildApiUrl } from '../../../../lib/api/base-url';
import { formatFileSize, resolveMediaUrl } from '../../../../lib/api/media-url';
import type { Event } from '../../../../lib/api/types';
import { getEventCategoryLabel, useEventCategories } from '../../../../lib/events/categories';
import { getEventLifecycle } from '../../../../lib/events/lifecycle';
import { formatEventDateTime } from '../../../../lib/events/timezone';
import { localized, localizedOptional } from '../../../../lib/i18n/localized';
import { isImageFile } from '../../../../lib/media/is-image-file';
import { EventRegistrationPanel } from '../../../../components/events/EventRegistrationPanel';
import { useAuth } from '../../../../contexts/AuthContext';
import { engagementApi } from '../../../../lib/api/engagement';

interface PracticalDetail {
  icon: string;
  label: string;
  value: string;
  href?: string;
}

const initialsFor = (name: string) =>
  name
    .split(/[\s—–-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('');

const PeopleList: React.FC<{ items: string[]; kind: 'speaker' | 'organizer' }> = ({
  items,
  kind,
}) => (
  <ul className="mt-6 grid grid-cols-1 gap-x-8 sm:grid-cols-2">
    {items.map((item, index) => (
      <li
        key={`${item}-${index}`}
        className="group flex items-center gap-4 border-t border-line/70 py-4 first:border-t-0 sm:[&:nth-child(2)]:border-t-0"
      >
        <span
          className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-full border font-display text-sm font-bold transition-colors ${
            kind === 'speaker'
              ? 'border-green/15 bg-green/[0.07] text-green group-hover:border-green group-hover:bg-green group-hover:text-white'
              : 'border-gold/40 bg-gold/[0.12] text-gold-ink group-hover:border-gold group-hover:bg-gold group-hover:text-green-deep'
          }`}
          aria-hidden="true"
        >
          {initialsFor(item) || <i className="ri-community-line" />}
        </span>
        <span className="text-[15px] font-semibold leading-5 text-ink">{item}</span>
      </li>
    ))}
  </ul>
);

export const EventDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const { isAuthenticated, user } = useAuth();
  const [event, setEvent] = useState<Event | null>(null);
  const [saved, setSaved] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const categories = useEventCategories();

  useEffect(() => {
    if (!id) return;

    const loadEvent = async () => {
      try {
        const response = await fetch(buildApiUrl(`/api/events/${id}`));
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

    void loadEvent();
  }, [id, navigate]);

  useEffect(() => {
    if (!id || !isAuthenticated || !user?.memberId) return;
    engagementApi.getSaved().then((response) => setSaved(Boolean(response.data?.some((item) => item.entityType === 'Event' && item.entityId === id)))).catch(() => undefined);
  }, [id, isAuthenticated, user?.memberId]);

  const toggleSaved = async () => {
    if (!id) return;
    if (saved) await engagementApi.removeSaved('Event', id); else await engagementApi.save('Event', id);
    setSaved((current) => !current);
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
        <div className="overflow-hidden bg-green-deep">
          <div className="container-page grid min-h-[430px] animate-pulse gap-10 py-14 lg:grid-cols-[1fr_290px] lg:items-center">
            <div className="space-y-5">
              <div className="h-3 w-40 bg-white/15" />
              <div className="h-16 max-w-3xl bg-white/10" />
              <div className="h-16 max-w-2xl bg-white/10" />
            </div>
            <div className="h-64 border border-white/10 bg-white/5" />
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
  const attachments = event.attachments ?? [];
  const speakers = event.speakers ?? [];
  const organizers = event.organizers ?? [];
  const imageAttachments = attachments.filter((attachment) =>
    isImageFile(attachment.contentType, attachment.fileName),
  );
  const fileAttachments = attachments.filter(
    (attachment) => !isImageFile(attachment.contentType, attachment.fileName),
  );

  const title = localized(event.title, event.titleEn, i18n.language);
  const description = localizedOptional(event.description, event.descriptionEn, i18n.language);
  const location = localizedOptional(event.location, event.locationEn, i18n.language);
  const typeLabel = getEventCategoryLabel(event.type, categories, i18n.language);
  const formatLabel = t(`public.news.evenements.format.${event.format || 'InPerson'}`);
  const formatIcon =
    event.format === 'Online'
      ? 'ri-live-line'
      : event.format === 'Hybrid'
        ? 'ri-links-line'
        : 'ri-map-pin-2-line';

  const statusForChip =
    lifecycle === 'ongoing'
      ? 'approved'
      : lifecycle === 'upcoming'
        ? 'pending'
        : lifecycle === 'past'
          ? 'past'
          : null;
  const statusLabel =
    lifecycle === 'ongoing'
      ? t('public.news.evenements.status.ongoing')
      : lifecycle === 'upcoming'
        ? t('public.news.evenements.status.upcoming')
        : lifecycle === 'past'
          ? t('public.news.evenements.status.past')
          : '';

  const eventDate = new Date(event.date);
  const timeZone = event.timeZone || 'America/Toronto';
  const calendarDay = new Intl.DateTimeFormat(locale, { day: '2-digit', timeZone }).format(eventDate);
  const calendarMonth = new Intl.DateTimeFormat(locale, { month: 'short', timeZone })
    .format(eventDate)
    .replace('.', '')
    .toUpperCase();
  const calendarYear = new Intl.DateTimeFormat(locale, { year: 'numeric', timeZone }).format(eventDate);
  const calendarWeekday = new Intl.DateTimeFormat(locale, { weekday: 'long', timeZone }).format(eventDate);
  const calendarTime = new Intl.DateTimeFormat(locale, {
    hour: '2-digit',
    minute: '2-digit',
    timeZoneName: 'short',
    timeZone,
  }).format(eventDate);

  const actionLabel =
    localizedOptional(event.ctaLabel, event.ctaLabelEn, i18n.language) ||
    (event.registrationUrl
      ? t('public.news.evenements.cta.register')
      : event.meetingLink
        ? t('public.news.evenements.joinMeeting')
        : t('public.news.evenements.cta.register'));
  const practicalDetails: PracticalDetail[] = [
    { icon: 'ri-calendar-line', label: t('public.news.evenements.startsAt'), value: formatDate(event.date, event.timeZone) },
    ...(event.endDate
      ? [{ icon: 'ri-time-line', label: t('public.news.evenements.endsAt'), value: formatDate(event.endDate, event.timeZone) }]
      : []),
    ...(location
      ? [{ icon: 'ri-map-pin-line', label: t('admin.common.location'), value: location }]
      : []),
    { icon: formatIcon, label: t('public.news.evenements.detail.access'), value: formatLabel },
    ...(event.capacity
      ? [{ icon: 'ri-group-line', label: t('admin.common.capacity'), value: t('admin.events.attendees', { count: event.capacity }) }]
      : []),
    ...(event.meetingLink
      ? [{
          icon: 'ri-video-chat-line',
          label: t('public.news.evenements.detail.meetingLink'),
          value: t('public.news.evenements.joinMeeting'),
          href: event.meetingLink,
        }]
      : []),
  ];

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <header className="public-grid-pattern public-header-enter relative isolate overflow-hidden bg-green-deep">
        <div className="pointer-events-none absolute -right-16 -top-36 h-[430px] w-[430px] rounded-full border-[70px] border-white/[0.035]" aria-hidden="true" />
        <div className="pointer-events-none absolute -bottom-24 left-[42%] font-display text-[240px] font-bold leading-none text-white/[0.025]" aria-hidden="true">
          {calendarDay}
        </div>

        <div className="container-page relative py-9 sm:py-12 lg:py-16">
          <Link to="/actualites/evenements" className="inline-flex min-h-11 items-center gap-2 text-[10px] font-bold uppercase tracking-[0.16em] text-white/65 transition-colors hover:text-gold">
            <i className="ri-arrow-left-line text-base" aria-hidden="true" />
            {t('public.news.evenements.backToList')}
          </Link>

          <div className="mt-7 grid items-end gap-10 lg:grid-cols-[minmax(0,1fr)_280px] lg:gap-16">
            <div className="max-w-[850px]">
              <div className="flex flex-wrap items-center gap-3">
                {statusForChip && <StatusChip status={statusForChip} label={statusLabel} />}
                <span className="inline-flex min-h-8 items-center gap-2 rounded-full border border-white/15 bg-white/[0.07] px-3 text-[10px] font-bold uppercase tracking-[0.12em] text-white/85">
                  <i className={`${formatIcon} text-gold`} aria-hidden="true" />
                  {formatLabel}
                </span>
              </div>

              {typeLabel && <p className="mt-7 text-[11px] font-bold uppercase tracking-[0.2em] text-gold">{typeLabel}</p>}
              <h1 className="mt-3 max-w-4xl font-display text-[40px] font-bold leading-[0.99] tracking-[-0.035em] text-white sm:text-[52px] lg:text-[66px]">
                {title}
              </h1>

              <div className="mt-8 flex flex-col gap-4 border-t border-white/15 pt-6 text-sm text-white/75 sm:flex-row sm:flex-wrap sm:gap-x-8">
                <span className="flex items-center gap-2.5">
                  <i className="ri-time-line text-lg text-gold" aria-hidden="true" />
                  {calendarWeekday} · {calendarTime}
                </span>
                {location && (
                  <span className="flex items-center gap-2.5">
                    <i className="ri-map-pin-line text-lg text-gold" aria-hidden="true" />
                    {location}
                  </span>
                )}
                {organizers[0] && (
                  <span className="flex items-center gap-2.5">
                    <i className="ri-community-line text-lg text-gold" aria-hidden="true" />
                    {organizers[0]}
                  </span>
                )}
              </div>
            </div>

            <div className="relative border border-white/15 bg-white/[0.075] p-6 text-white backdrop-blur-sm sm:p-7">
              <div className="absolute inset-y-0 left-0 w-1 bg-gold" aria-hidden="true" />
              <p className="text-[10px] font-bold uppercase tracking-[0.18em] text-white/55">{t('public.news.evenements.detail.eventDate')}</p>
              <div className="mt-4 flex items-end gap-4">
                <span className="font-display text-[76px] font-bold leading-[0.78] tracking-[-0.06em] text-white">{calendarDay}</span>
                <span className="pb-0.5">
                  <strong className="block text-lg font-bold tracking-[0.08em] text-gold">{calendarMonth}</strong>
                  <span className="mt-0.5 block text-sm text-white/55">{calendarYear}</span>
                </span>
              </div>
              <p className="mt-6 border-t border-white/15 pt-4 text-sm capitalize text-white/80">{calendarWeekday} · {calendarTime}</p>
              {event.endDate && <p className="mt-2 text-xs leading-5 text-white/55">{t('public.news.evenements.endsAt')} · {formatDate(event.endDate, event.timeZone)}</p>}
            </div>
          </div>

          {isPast && <div className="mt-8 max-w-3xl border-l-2 border-gold bg-white/[0.07] px-5 py-4 text-sm text-white/70">{t('public.news.evenements.pastNotice')}</div>}
        </div>
      </header>

      {event.imageUrl && (
        <div className="container-page relative z-10 -mb-6 -mt-7 sm:-mt-10">
          <div className="relative h-[280px] overflow-hidden border border-white/20 bg-green-deep shadow-[0_24px_60px_rgba(0,59,27,.16)] sm:h-[390px] lg:h-[470px]">
            <img
              src={resolveMediaUrl(event.imageUrl)}
              alt=""
              className={`h-full w-full object-cover ${isPast ? 'grayscale-[30%]' : ''}`}
              onError={(imageEvent) => { imageEvent.currentTarget.style.display = 'none'; }}
            />
            <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-green-deep/30 via-transparent to-transparent" />
          </div>
        </div>
      )}

      <main className="bg-surface py-12 sm:py-16 lg:py-20">
        <div className="container-page grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-14">
          <article className="order-2 min-w-0 lg:order-1 lg:col-span-8">
            <section aria-labelledby="event-about-title">
              <div className="flex items-center gap-4"><span className="font-display text-2xl font-bold text-red-link">01</span><div className="h-px flex-1 bg-line" /></div>
              <p className="mt-7 text-[10px] font-bold uppercase tracking-[0.18em] text-red-link">{typeLabel || t('public.news.evenements.detail.communityEvent')}</p>
              <h2 id="event-about-title" className="mt-2 font-display text-[32px] font-bold leading-tight text-green sm:text-[40px]">{t('public.news.evenements.detail.about')}</h2>
              {description ? (
                <RichTextContent value={description} className="mt-6 max-w-[68ch] !text-[17px] !leading-8" />
              ) : (
                <p className="mt-6 text-ink-variant">{t('public.news.evenements.detail.noDescription')}</p>
              )}
            </section>

            {(speakers.length > 0 || organizers.length > 0) && (
              <section className="mt-14 border-t border-line pt-10" aria-labelledby="event-people-title">
                <div className="flex items-center gap-4"><span className="font-display text-2xl font-bold text-red-link">02</span><div className="h-px flex-1 bg-line" /></div>
                <h2 id="event-people-title" className="mt-7 font-display text-[30px] font-bold text-green sm:text-[36px]">{t('public.news.evenements.detail.people')}</h2>
                <div className="mt-8 grid grid-cols-1 gap-10 xl:grid-cols-2 xl:gap-12">
                  {speakers.length > 0 && (
                    <div>
                      <p className="flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.16em] text-red-link"><i className="ri-user-voice-line text-base" aria-hidden="true" />{t('public.news.evenements.speakers')}</p>
                      <PeopleList items={speakers} kind="speaker" />
                    </div>
                  )}
                  {organizers.length > 0 && (
                    <div>
                      <p className="flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.16em] text-red-link"><i className="ri-community-line text-base" aria-hidden="true" />{t('public.news.evenements.organizers')}</p>
                      <PeopleList items={organizers} kind="organizer" />
                    </div>
                  )}
                </div>
              </section>
            )}

            {(event.media?.length ?? 0) > 0 && <EventMediaGallery media={event.media ?? []} />}

            {(imageAttachments.length > 0 || fileAttachments.length > 0) && (
              <section className="mt-16 border-t border-line pt-10">
                <div className="flex items-center gap-4"><span className="font-display text-2xl font-bold text-red-link">03</span><div className="h-px flex-1 bg-line" /></div>
                <h2 className="mt-7 font-display text-[30px] font-bold text-green sm:text-[36px]">{t('public.news.evenements.attachments')}</h2>
                {imageAttachments.length > 0 && (
                  <div className="mt-8"><ImageCarousel images={imageAttachments.map((attachment) => ({ id: attachment.id, url: attachment.url, alt: attachment.fileName }))} /></div>
                )}
                {fileAttachments.length > 0 && (
                  <ul className="mt-8 divide-y divide-line border-y border-line">
                    {fileAttachments.map((attachment) => (
                      <li key={attachment.id} className="flex flex-wrap items-center justify-between gap-4 py-5">
                        <div className="flex min-w-0 items-center gap-4">
                          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-green/[0.08] text-green"><i className="ri-file-text-line text-lg" aria-hidden="true" /></span>
                          <div className="min-w-0">
                            <p className="truncate text-[15px] font-semibold text-ink">{attachment.fileName}</p>
                            <p className="mt-1 text-[10px] font-bold uppercase tracking-[0.12em] text-ink-variant">{(attachment.contentType || '').split('/').pop() || 'FICHIER'} · {formatFileSize(attachment.sizeBytes)}</p>
                          </div>
                        </div>
                        <a href={resolveMediaUrl(attachment.url)} target="_blank" rel="noopener noreferrer" className="inline-flex min-h-11 items-center gap-2 text-[11px] font-bold uppercase tracking-[0.12em] text-red-link transition-colors hover:text-green">
                          {t('public.services.documents.download')}<i className="ri-download-line text-base" aria-hidden="true" />
                        </a>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            )}
          </article>

          <aside className="order-1 lg:order-2 lg:col-span-4">
            <div className="lg:sticky lg:top-24">
              <section className="relative overflow-hidden bg-green-deep p-6 text-white sm:p-8" aria-labelledby="registration-title">
                <div className="absolute right-0 top-0 h-24 w-24 translate-x-8 -translate-y-8 rounded-full border-[18px] border-white/5" aria-hidden="true" />
                <p className="text-[10px] font-bold uppercase tracking-[0.18em] text-gold">{statusLabel}</p>
                <h2 id="registration-title" className="mt-2 font-display text-[27px] font-bold leading-tight text-white">{t('public.news.evenements.detail.registration')}</h2>
                {event.registrationDeadline && (
                  <div className="mt-5 border-l-2 border-gold/70 pl-4">
                    <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-white/50">{t('public.news.evenements.registrationDeadline')}</p>
                    <p className="mt-1 text-sm leading-6 text-white/80">{formatDate(event.registrationDeadline, event.timeZone)}</p>
                  </div>
                )}
                {isAuthenticated && user?.memberId && <button type="button" onClick={toggleSaved} className={`mt-5 inline-flex min-h-11 w-full items-center justify-center gap-2 rounded-control border text-[10px] font-bold uppercase tracking-[.11em] transition ${saved ? 'border-gold bg-gold text-green-deep' : 'border-white/20 text-white hover:border-gold hover:text-gold'}`}><i className={saved ? 'ri-bookmark-fill' : 'ri-bookmark-line'} />{saved ? (i18n.language.startsWith('fr') ? 'Événement enregistré' : 'Event saved') : (i18n.language.startsWith('fr') ? 'Enregistrer pour plus tard' : 'Save for later')}</button>}
                <EventRegistrationPanel event={event} isPast={isPast} externalLabel={actionLabel} />
              </section>

              <section className="border-x border-b border-line bg-background px-6 py-7 sm:px-8" aria-labelledby="practical-title">
                <h2 id="practical-title" className="font-display text-[23px] font-bold text-green">{t('public.news.evenements.detail.practical')}</h2>
                <dl className="mt-5 divide-y divide-line/75">
                  {practicalDetails.map((detail) => (
                    <div key={`${detail.label}-${detail.value}`} className="grid grid-cols-[34px_1fr] gap-3 py-4 first:pt-1">
                      <span className="flex h-8 w-8 items-center justify-center rounded-full bg-green/[0.07] text-green" aria-hidden="true"><i className={`${detail.icon} text-base`} /></span>
                      <div>
                        <dt className="text-[9px] font-bold uppercase tracking-[0.14em] text-ink-variant">{detail.label}</dt>
                        <dd className="mt-1 text-sm font-medium leading-5 text-ink">
                          {detail.href ? (
                            <a
                              href={detail.href}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="group inline-flex min-h-8 items-center gap-2 font-bold text-green underline decoration-gold decoration-2 underline-offset-4 transition-colors hover:text-red-link focus-visible:outline-green"
                            >
                              {detail.value}
                              <i className="ri-arrow-right-up-line text-base transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5" aria-hidden="true" />
                            </a>
                          ) : detail.value}
                        </dd>
                      </div>
                    </div>
                  ))}
                </dl>
              </section>
            </div>
          </aside>
        </div>
      </main>

      <Footer />
    </div>
  );
};

export default EventDetailPage;
