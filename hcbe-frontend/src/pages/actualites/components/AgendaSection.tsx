import { eventsApi } from '../../../lib/api/events';
import type { Event } from '../../../lib/api/types';
import { localized, localizedOptional } from '../../../lib/i18n/localized';
import { ArrowLink, EmptyState } from '../../../components/ui';

const AgendaSection = () => {
  const { t, i18n } = useTranslation();
  const [selectedMonth, setSelectedMonth] = useState('tous');
  const [events, setEvents] = useState<Event[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    loadEvents();
  }, []);

  const loadEvents = async () => {
    try {
      setIsLoading(true);
      const response = await eventsApi.getEvents();
      if (response.success && response.data) {
        // Filter to only show active/public events
        const publicEvents = response.data.filter(event =>
          event.status === 'Active' || event.status === 'À venir'
        );
        setEvents(publicEvents);
      } else {
        setError('Failed to load events');
      }
    } catch (error) {
      console.error('Error loading events:', error);
      setError('Error loading events');
    } finally {
      setIsLoading(false);
    }
  };

  const locale = i18n.language.startsWith('en') ? 'en-CA' : 'fr-CA';

  const formatDate = (dateString: string) =>
    new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'long', year: 'numeric' }).format(new Date(dateString));

  const formatTime = (dateString: string) =>
    new Intl.DateTimeFormat(locale, { hour: '2-digit', minute: '2-digit' }).format(new Date(dateString));

  // Generate months dynamically from the loaded events
  const months = (() => {
    const labelsByKey = new Map<string, string>();
    events.forEach((event) => {
      const date = new Date(event.date);
      const monthKey = date.toISOString().slice(5, 7); // MM format
      if (!labelsByKey.has(monthKey)) {
        const label = new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' }).format(date);
        labelsByKey.set(monthKey, label.charAt(0).toUpperCase() + label.slice(1));
      }
    });

    const sortedMonths = Array.from(labelsByKey.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([value, label]) => ({ value, label }));

    return [{ value: 'tous', label: t('public.news.evenements.filter.allMonths') }, ...sortedMonths];
  })();

  const filteredEvents =
    selectedMonth === 'tous'
      ? events
      : events.filter((event) => event.date.split('T')[0].split('-')[1] === selectedMonth);

  const renderContent = () => {
    if (isLoading) {
      return (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {[1, 2, 3].map((item) => (
            <div key={item} className="space-y-3 rounded-xl bg-background p-5">
              <div className="h-4 w-24 animate-pulse bg-surface-container" />
              <div className="h-6 w-2/3 animate-pulse bg-surface-container" />
              <div className="h-4 w-full animate-pulse bg-surface-container" />
            </div>
          ))}
        </div>
      );
    }

    if (error) {
      return (
        <EmptyState
          tone="error"
          icon="ri-error-warning-line"
          title={t('public.news.evenements.error.load')}
        />
      );
    }

    if (filteredEvents.length === 0) {
      return (
        <EmptyState
          icon="ri-calendar-event-line"
          title={t('public.news.evenements.empty.title')}
          description={t('public.news.evenements.empty.current')}
        />
      );
    }

    return (
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {filteredEvents.slice(0, 3).map((event) => {
          const location = localizedOptional(event.location, event.locationEn, i18n.language);
          return (
            <article key={event.id} className="group h-full rounded-xl border border-green/10 bg-white p-5 transition-all hover:-translate-y-0.5 hover:shadow-[0_12px_30px_rgba(0,59,27,.07)]">
              <div className="flex items-start gap-4">
                <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-green-deep text-gold">
                  <i className="ri-calendar-event-line text-lg" aria-hidden="true" />
                </span>
                <div className="min-w-0">
                  <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-red-link">{formatDate(event.date)}</p>
                  <h3 className="mt-2 font-display text-xl font-semibold leading-tight text-green">
                    {localized(event.title, event.titleEn, i18n.language)}
                  </h3>
                  <p className="mt-2 text-sm text-ink-variant">
                    {formatTime(event.date)}
                    {location ? ` · ${location}` : ''}
                  </p>
                  {localizedOptional(event.description, event.descriptionEn, i18n.language) && (
                    <p className="mt-3 line-clamp-2 text-sm leading-6 text-ink-variant">
                      {localized(event.description, event.descriptionEn, i18n.language)}
                    </p>
                  )}
                  <ArrowLink to={`/actualites/evenements/${event.id}`} tone="red" className="mt-4">
                    {t('public.news.evenements.cta.details')}
                  </ArrowLink>
                </div>
              </div>
            </article>
          );
        })}
      </div>
    );
  };

  return (
    <div>
      <div className="mb-6 flex flex-wrap gap-2">
        {months.map((month) => (
          <button
            key={month.value}
            type="button"
            onClick={() => setSelectedMonth(month.value)}
            className={`min-h-[40px] rounded-full border px-4 py-2 text-[10px] font-bold uppercase tracking-[0.1em] transition-colors ${
              selectedMonth === month.value
                ? 'border-green bg-green text-white'
                : 'border-line text-ink-variant hover:border-green'
            }`}
          >
            {month.label}
          </button>
        ))}
      </div>
      {renderContent()}
    </div>
  );
};

export default AgendaSection;
