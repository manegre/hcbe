import Navbar from '../../../components/feature/Navbar';
import Footer from '../../../components/feature/Footer';
import { buildApiUrl } from '../../../lib/api/base-url';
import { resolveMediaUrl } from '../../../lib/api/media-url';
import {
  getEventLifecycle,
  isPublicAgendaEvent,
  sortEventsForPublic,
  type EventLifecycle,
} from '../../../lib/events/lifecycle';
import type { Event } from '../../../lib/api/types';
import { localized, localizedOptional } from '../../../lib/i18n/localized';
import { ArrowLink, EmptyState, PageHeader, StatusChip, Tag } from '../../../components/ui';

type PublicFilter = 'current' | 'past' | 'all';

const filterTabs: { id: PublicFilter; labelKey: string }[] = [
  { id: 'current', labelKey: 'public.news.evenements.filter.current' },
  { id: 'past', labelKey: 'public.news.evenements.filter.past' },
  { id: 'all', labelKey: 'public.news.evenements.filter.all' },
];

export const EvenementsPage = () => {
  const { t, i18n } = useTranslation();
  const [events, setEvents] = useState<Event[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [filter, setFilter] = useState<PublicFilter>('current');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadEvents();
  }, []);

  const loadEvents = async () => {
    try {
      setError(null);
      const response = await fetch(buildApiUrl('/api/events'));
      const result = await response.json();
      if (result.success && Array.isArray(result.data)) {
        const published = result.data.filter((event: Event) => isPublicAgendaEvent(event));
        setEvents(sortEventsForPublic(published));
      } else {
        setError(t('public.news.evenements.error.unavailable'));
      }
    } catch (err) {
      console.error('Error loading events:', err);
      setError(t('public.news.evenements.error.load'));
    } finally {
      setIsLoading(false);
    }
  };

  const filteredEvents = useMemo(() => {
    if (filter === 'all') return events;
    if (filter === 'past') {
      return events.filter((event) => getEventLifecycle(event) === 'past');
    }
    return events.filter((event) => {
      const life = getEventLifecycle(event);
      return life === 'upcoming' || life === 'ongoing';
    });
  }, [events, filter]);

  const locale = i18n.language.startsWith('en') ? 'en-CA' : 'fr-CA';

  const formatDay = (dateString: string) =>
    new Intl.DateTimeFormat(locale, { day: '2-digit' }).format(new Date(dateString));

  const formatMonthYear = (dateString: string) =>
    new Intl.DateTimeFormat(locale, { month: 'short', year: 'numeric' }).format(new Date(dateString));

  const lifecycleLabel = (lifecycle: EventLifecycle) => {
    if (lifecycle === 'ongoing') return t('public.news.evenements.status.ongoing');
    if (lifecycle === 'upcoming') return t('public.news.evenements.status.upcoming');
    if (lifecycle === 'past') return t('public.news.evenements.status.past');
    return '';
  };

  const emptyMessageKey =
    filter === 'current'
      ? 'public.news.evenements.empty.current'
      : filter === 'past'
        ? 'public.news.evenements.empty.past'
        : 'public.news.evenements.empty.all';

  const isVirtual = (event: Event) => {
    const type = (event.type || '').toLowerCase();
    return type.includes('virtuel') || type.includes('virtual') || Boolean(event.meetingLink);
  };

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <PageHeader
        title={t('public.news.evenements.title')}
        description={t('public.news.evenements.subtitle')}
        actions={
          <ArrowLink to="/actualites/annonces" tone="gold">
            {t('public.news.hero.cta.announcements')}
          </ArrowLink>
        }
      />

      <main className="bg-surface-container py-12 md:py-16">
        <div className="container-page">
          <div className="rounded-[18px] border border-green/10 bg-white p-2 shadow-[0_14px_35px_rgba(0,59,27,.06)]">
            <nav className="flex flex-wrap gap-2" aria-label={t('public.news.evenements.title')}>
              {filterTabs.map((tab) => (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => setFilter(tab.id)}
                  aria-pressed={filter === tab.id}
                  className={`min-h-[42px] rounded-full px-5 py-2 text-[10px] font-bold uppercase tracking-[0.12em] transition-colors ${
                    filter === tab.id
                      ? 'bg-green-deep text-white shadow-sm'
                      : 'text-ink-variant hover:bg-background hover:text-green'
                  }`}
                >
                  {t(tab.labelKey)}
                </button>
              ))}
            </nav>
          </div>

          {error ? (
            <div className="mt-10">
              <EmptyState tone="error" icon="ri-error-warning-line" title={error} />
            </div>
          ) : isLoading ? (
            <div className="mt-8 space-y-5">
              {[1, 2, 3].map((item) => (
                <div
                  key={item}
                  className="grid grid-cols-1 gap-6 rounded-[18px] border border-green/10 bg-white p-6 md:grid-cols-[96px_200px_1fr]"
                >
                  <div className="h-10 w-16 animate-pulse bg-surface-container" />
                  <div className="h-[140px] w-full animate-pulse bg-surface-container" />
                  <div className="space-y-3">
                    <div className="h-4 w-1/3 animate-pulse bg-surface-container" />
                    <div className="h-6 w-2/3 animate-pulse bg-surface-container" />
                    <div className="h-4 w-full animate-pulse bg-surface-container" />
                  </div>
                </div>
              ))}
            </div>
          ) : filteredEvents.length === 0 ? (
            <div className="mt-10">
              <EmptyState
                icon="ri-calendar-event-line"
                title={t('public.news.evenements.empty.title')}
                description={t(emptyMessageKey)}
              />
            </div>
          ) : (
            <div className="mt-8 space-y-5">
              {filteredEvents.map((event) => {
                const lifecycle = getEventLifecycle(event);
                const coverUrl = resolveMediaUrl(event.imageUrl);
                const location = localizedOptional(event.location, event.locationEn, i18n.language);
                const description = localizedOptional(
                  event.description,
                  event.descriptionEn,
                  i18n.language,
                );

                return (
                  <article
                    key={event.id}
                    className="group grid grid-cols-1 gap-6 overflow-hidden rounded-[18px] border border-green/10 bg-white p-5 transition-all hover:-translate-y-0.5 hover:shadow-[0_20px_48px_rgba(0,59,27,.11)] md:grid-cols-[96px_200px_1fr] md:p-6"
                  >
                    <div className="flex items-baseline gap-2 md:block">
                      <p className="font-display text-headline-lg tabular-nums text-red-link">
                        {formatDay(event.date)}
                      </p>
                      <p className="text-label-md uppercase text-ink-variant">
                        {formatMonthYear(event.date)}
                      </p>
                    </div>
                    <div className="relative flex h-[140px] w-full items-center justify-center overflow-hidden rounded-xl bg-green-deep text-gold">
                      <i className="ri-calendar-event-line text-3xl" aria-hidden="true" />
                      {coverUrl && (
                        <img
                          src={coverUrl}
                          alt=""
                          className="absolute inset-0 h-full w-full object-cover transition-transform duration-500 group-hover:scale-[1.02]"
                          onError={(event) => {
                            event.currentTarget.style.display = 'none';
                          }}
                        />
                      )}
                    </div>
                    <div>
                      {location && (
                        <p className="flex items-center gap-2 text-label-md uppercase text-ink-variant">
                          <i className="ri-map-pin-line text-gold-ink" aria-hidden="true"></i>
                          {location}
                        </p>
                      )}
                      <h3 className="mt-2 font-display text-headline-md text-green">
                        {localized(event.title, event.titleEn, i18n.language)}
                      </h3>
                      {description && (
                        <p className="mt-3 max-w-3xl text-body-md text-ink-variant">{description}</p>
                      )}
                      {lifecycle === 'past' && (
                        <p className="mt-4 rounded-r-lg border-l-2 border-gold bg-background p-4 text-body-md text-ink-variant">
                          {t('public.news.evenements.pastNotice')}
                        </p>
                      )}
                      <div className="mt-4 flex flex-wrap items-center gap-4">
                        <StatusChip
                          status={
                            lifecycle === 'ongoing' ? 'approved' : lifecycle === 'upcoming' ? 'pending' : 'past'
                          }
                          label={lifecycleLabel(lifecycle)}
                        />
                        {isVirtual(event) && (
                          <Tag>
                            <i className="ri-video-line mr-1" aria-hidden="true"></i>
                            {t('public.news.evenements.status.virtual')}
                          </Tag>
                        )}
                        <ArrowLink to={`/actualites/evenements/${event.id}`} tone="red">
                          {lifecycle === 'past'
                            ? t('public.news.evenements.cta.recap')
                            : t('public.news.evenements.cta.details')}
                        </ArrowLink>
                      </div>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </div>
      </main>

      <Footer />
    </div>
  );
};

export default EvenementsPage;
