import { eventsApi } from '../../../lib/api/events';
import type { Event } from '../../../lib/api/types';
import { isCurrentOrUpcomingEvent } from '../../../lib/events/lifecycle';
import { localized, localizedOptional } from '../../../lib/i18n/localized';
import { ArrowLink, EmptyState, SectionHeading } from '../../../components/ui';

const MAX_EVENTS = 3;

const UpcomingEventsSection = () => {
  const { t, i18n } = useTranslation();
  const [events, setEvents] = useState<Event[]>([]);
  const [isReady, setIsReady] = useState(false);
  const [hasError, setHasError] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const loadEvents = async () => {
      try {
        const response = await eventsApi.getEvents();
        if (cancelled) return;

        if (response.success && response.data) {
          const upcoming = response.data
            .filter(isCurrentOrUpcomingEvent)
            .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())
            .slice(0, MAX_EVENTS);
          setEvents(upcoming);
        }
      } catch (error) {
        console.error('Error loading home upcoming events:', error);
        if (!cancelled) {
          setHasError(true);
        }
      } finally {
        if (!cancelled) {
          setIsReady(true);
        }
      }
    };

    loadEvents();
    return () => {
      cancelled = true;
    };
  }, []);

  const locale = i18n.language.startsWith('fr') ? 'fr-CA' : 'en-CA';

  const formatDate = (dateString: string) =>
    new Intl.DateTimeFormat(locale, {
      weekday: 'short',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    }).format(new Date(dateString));

  return (
    <section className="bg-paper py-24">
      <div className="container-page">
        <SectionHeading
          title={t('public.home.events.title')}
          description={t('public.home.events.subtitle')}
          action={
            <ArrowLink to="/actualites/evenements" tone="red">
              {t('public.home.events.viewAll')}
            </ArrowLink>
          }
        />

        {!isReady ? (
          <div className="border-b border-line">
            {[1, 2, 3].map((item) => (
              <div key={item} className="grid grid-cols-1 gap-6 border-t border-line py-8 md:grid-cols-[120px_1fr]">
                <div className="h-4 w-24 animate-pulse bg-surface-container" />
                <div className="space-y-3">
                  <div className="h-6 w-2/3 animate-pulse bg-surface-container" />
                  <div className="h-4 w-full animate-pulse bg-surface-container" />
                  <div className="h-4 w-32 animate-pulse bg-surface-container" />
                </div>
              </div>
            ))}
          </div>
        ) : hasError ? (
          <EmptyState
            tone="error"
            icon="ri-error-warning-line"
            title={t('public.home.events.error.title')}
            description={t('public.home.events.error.description')}
          />
        ) : events.length === 0 ? (
          <EmptyState
            icon="ri-calendar-event-line"
            title={t('public.home.events.empty.title')}
            description={t('public.home.events.empty.description')}
          />
        ) : (
          <div className="border-b border-line">
            {events.map((event) => (
              <article
                key={event.id}
                className="grid grid-cols-1 gap-6 border-t border-line py-8 md:grid-cols-[120px_1fr]"
              >
                <p className="text-label-md uppercase text-red-link">{formatDate(event.date)}</p>
                <div>
                  <h3 className="font-display text-headline-md text-ink">
                    {localized(event.title, event.titleEn, i18n.language)}
                  </h3>
                  {localizedOptional(event.description, event.descriptionEn, i18n.language) && (
                    <p className="mt-2 line-clamp-3 text-body-md text-ink-variant">
                      {localized(event.description, event.descriptionEn, i18n.language)}
                    </p>
                  )}
                  <ArrowLink to={`/actualites/evenements/${event.id}`} tone="red" className="mt-4">
                    {t('public.home.events.details')}
                  </ArrowLink>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
    </section>
  );
};

export default UpcomingEventsSection;
