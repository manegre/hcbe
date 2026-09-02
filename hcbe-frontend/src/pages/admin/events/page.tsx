import { eventsApi } from '../../../lib/api/events';
import type { Event } from '../../../lib/api/types';
import { getPublicationLabel, translateEventLifecycle } from '../../../lib/i18n/adminStatus';
import { getEventLifecycle } from '../../../lib/events/lifecycle';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { Button, Field, StatusChip, Td, inputClasses } from '../../../components/ui';
import { getEventCategoryLabel, useEventCategories } from '../../../lib/events/categories';
import { formatEventDateTime } from '../../../lib/events/timezone';

const eventLifecycleChipStatus = (event: Event): 'published' | 'draft' | 'past' | 'rejected' => {
  const lifecycle = getEventLifecycle(event);
  if (lifecycle === 'past') return 'past';
  if (lifecycle === 'draft') return 'draft';
  if (lifecycle === 'cancelled') return 'rejected';
  return 'published';
};

export const AdminEventsList = () => {
  const [events, setEvents] = useState<Event[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('all');
  const [sortBy, setSortBy] = useState('date');
  const { t, i18n } = useTranslation();
  const categories = useEventCategories(true);

  useEffect(() => {
    loadEvents();
  }, []);

  const loadEvents = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const response = await eventsApi.getEventsForAdmin();
      if (response.success && response.data) {
        setEvents(response.data);
      } else {
        setError(response.message || t('admin.events.errorLoadList'));
      }
    } catch (err) {
      console.error('Error loading events:', err);
      setError(err instanceof Error ? err.message : t('admin.events.errorLoadList'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteEvent = async (id: string, title: string) => {
    if (!window.confirm(t('admin.common.confirmDelete', { name: title }))) {
      return;
    }

    try {
      const response = await eventsApi.deleteEvent(id);
      if (response.success) {
        setEvents(events.filter((event) => event.id !== id));
      }
    } catch (err) {
      console.error('Error deleting event:', err);
      alert(t('admin.events.errorDelete'));
    }
  };

  const filterOptions = [
    { value: 'all', label: t('admin.events.filterAll') },
    { value: 'upcoming', label: t('admin.eventLifecycle.upcoming') },
    { value: 'ongoing', label: t('admin.eventLifecycle.ongoing') },
    { value: 'past', label: t('admin.eventLifecycle.past') },
    { value: 'draft', label: t('admin.eventLifecycle.draft') },
    { value: 'cancelled', label: t('admin.eventLifecycle.cancelled') },
  ];

  const filteredEvents = events.filter((event) => {
    if (filter === 'all') return true;
    return getEventLifecycle(event) === filter;
  });

  const sortedEvents = [...filteredEvents].sort((a, b) => {
    if (sortBy === 'title') return a.title.localeCompare(b.title);
    if (sortBy === 'created') {
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    }
    return new Date(a.date).getTime() - new Date(b.date).getTime();
  });

  const sortOptions = [
    { value: 'date', label: t('admin.events.sortDate') },
    { value: 'title', label: t('admin.events.sortTitle') },
    { value: 'created', label: t('admin.events.sortCreated') },
  ];

  const currentFilterLabel =
    filterOptions.find((option) => option.value === filter)?.label ?? filter;

  const locale = i18n.language.startsWith('fr') ? 'fr-CA' : 'en-CA';
  const formatListDate = (event: Event) =>
    formatEventDateTime(event.date, locale, event.timeZone, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });

  const toolbar = (
    <>
      <Button to="/admin/events/categories" variant="secondary">
        <i className="ri-price-tag-3-line" aria-hidden="true" />
        {t('admin.events.categories.manage')}
      </Button>
      <Field label={t('admin.common.filterBy')} htmlFor="event-filter">
        <select
          id="event-filter"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          className={inputClasses}
        >
          {filterOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </Field>
      <Field label={t('admin.common.sortBy')} htmlFor="event-sort">
        <select
          id="event-sort"
          value={sortBy}
          onChange={(e) => setSortBy(e.target.value)}
          className={inputClasses}
        >
          {sortOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </Field>
    </>
  );

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <AdminListPage
      title={t('admin.events.title')}
      count={error ? undefined : sortedEvents.length}
      createLabel={t('admin.events.create')}
      createPath="/admin/events/create"
      toolbar={toolbar}
      columns={[
        { key: 'event', label: t('admin.events.colEvent') },
        { key: 'dateLocation', label: t('admin.events.colDateLocation') },
        { key: 'lifecycle', label: t('admin.events.colLifecycle') },
        { key: 'details', label: t('admin.events.colDetails') },
        { key: 'actions', label: t('admin.common.actions'), align: 'right' },
      ]}
      isEmpty={sortedEvents.length === 0}
      emptyTitle={t('admin.events.emptyTitle')}
      emptyDescription={
        filter === 'all'
          ? t('admin.events.emptyAll')
          : t('admin.events.emptyFilter', { filter: currentFilterLabel })
      }
      error={error ?? undefined}
    >
      {sortedEvents.map((event) => (
        <tr key={event.id} className="transition-colors hover:bg-surface-container">
          <Td className="text-ink">
            <div className="font-medium">{event.title}</div>
            {event.description && (
              <div className="mt-1 max-w-xs truncate text-body-md text-ink-variant">
                {event.description}
              </div>
            )}
          </Td>
          <Td>
            <div>{formatListDate(event)}</div>
            {event.location && <div className="text-ink-variant">{event.location}</div>}
          </Td>
          <Td>
            <div className="space-y-1">
              <StatusChip
                status={eventLifecycleChipStatus(event)}
                label={translateEventLifecycle(event, t)}
              />
              <div className="text-body-md text-ink-variant">{getPublicationLabel(event.status, t)}</div>
            </div>
          </Td>
          <Td>
            {event.type && (
              <div>
                {t('admin.common.type')}: {getEventCategoryLabel(event.type, categories, i18n.language)}
              </div>
            )}
            <div>{t(`admin.events.format.${event.format || 'InPerson'}`)}</div>
            {event.zone && (
              <div>
                {t('admin.common.zone')}: {event.zone}
              </div>
            )}
            {event.capacity && (
              <div>
                {t('admin.common.capacity')}: {event.capacity}
              </div>
            )}
          </Td>
          <Td align="right">
            <div className="inline-flex items-center justify-end gap-1">
              <Link
                to={`/admin/events/${event.id}`}
                aria-label={t('admin.common.view')}
                title={t('admin.common.view')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-eye-line text-lg" aria-hidden="true" />
              </Link>
              <Link
                to={`/admin/events/${event.id}/edit`}
                aria-label={t('admin.common.edit')}
                title={t('admin.common.edit')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-edit-line text-lg" aria-hidden="true" />
              </Link>
              <button
                type="button"
                onClick={() => handleDeleteEvent(event.id, event.title)}
                aria-label={t('admin.common.delete')}
                title={t('admin.common.delete')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center rounded-control text-error transition-colors hover:text-error-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-error"
              >
                <i className="ri-delete-bin-line text-lg" aria-hidden="true" />
              </button>
            </div>
          </Td>
        </tr>
      ))}
    </AdminListPage>
  );
};
