import { eventsApi } from '../../../lib/api/events';
import { membersApi } from '../../../lib/api/members';
import { membershipApplicationsApi } from '../../../lib/api/membership-applications';
import { newsApi } from '../../../lib/api/news';
import { projectsApi } from '../../../lib/api/projects';
import type { Event, MembershipApplicationDto, NewsArticle } from '../../../lib/api/types';
import { getEventLifecycle, isCurrentOrUpcomingEvent } from '../../../lib/events/lifecycle';
import { formatEventDateTime } from '../../../lib/events/timezone';
import { translateEventLifecycle } from '../../../lib/i18n/adminStatus';
import { localized } from '../../../lib/i18n/localized';
import { ArrowLink, Button, DataTable, StatusChip, Td } from '../../../components/ui';

const eventLifecycleChipStatus = (event: Event): 'published' | 'draft' | 'past' | 'rejected' => {
  const lifecycle = getEventLifecycle(event);
  if (lifecycle === 'past') return 'past';
  if (lifecycle === 'draft') return 'draft';
  if (lifecycle === 'cancelled') return 'rejected';
  return 'published';
};

type DashboardStats = {
  upcomingEvents: number;
  pendingApplications: number;
  members: number;
  publishedNews: number;
  activeProjects: number;
};

const emptyStats: DashboardStats = {
  upcomingEvents: 0,
  pendingApplications: 0,
  members: 0,
  publishedNews: 0,
  activeProjects: 0,
};

const isActiveProjectStatus = (status?: string | null) => {
  const normalized = (status ?? '').toLowerCase();
  return (
    normalized === 'en cours' ||
    normalized === 'actif' ||
    normalized === 'active' ||
    normalized === 'in progress' ||
    normalized === 'planification' ||
    normalized === 'planning'
  );
};

const isPublishedNews = (article: NewsArticle) => {
  const normalized = (article.status ?? '').toLowerCase();
  return normalized === 'published' || normalized === 'publié' || normalized === 'publie';
};

const loadList = async <T,>(
  request: Promise<{ success: boolean; data?: T[] | null; message?: string }>,
  label: string,
): Promise<{ data: T[]; ok: boolean; error?: string }> => {
  try {
    const response = await request;

    if (!response.success || !Array.isArray(response.data)) {
      console.error(`Dashboard load incomplete (${label}):`, response.message ?? response);
      return { data: [], ok: false, error: response.message ?? 'Invalid response' };
    }

    return { data: response.data, ok: true };
  } catch (error) {
    console.error(`Dashboard load failed (${label}):`, error);
    return {
      data: [],
      ok: false,
      error: error instanceof Error ? error.message : String(error),
    };
  }
};

export const AdminDashboard = () => {
  const { t, i18n } = useTranslation();
  const [stats, setStats] = useState<DashboardStats>(emptyStats);
  const [upcomingEvents, setUpcomingEvents] = useState<Event[]>([]);
  const [pendingApplications, setPendingApplications] = useState<MembershipApplicationDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [failedSources, setFailedSources] = useState<string[]>([]);
  const [dashboardError, setDashboardError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let cancelled = false;

    const loadDashboard = async () => {
      setIsLoading(true);
      setFailedSources([]);
      setDashboardError(null);

      try {
        const loads = await Promise.all([
          loadList(eventsApi.getEvents(), 'events').then((load) => ({ key: 'events', ...load })),
          loadList(membershipApplicationsApi.getAll('Pending'), 'membership-applications').then(
            (load) => ({ key: 'applications', ...load }),
          ),
          loadList(membersApi.getAllMembers(), 'members').then((load) => ({ key: 'members', ...load })),
          loadList(newsApi.getNewsForAdmin(), 'news').then((load) => ({ key: 'news', ...load })),
          loadList(projectsApi.getProjectsForAdmin(), 'projects').then((load) => ({
            key: 'projects',
            ...load,
          })),
        ]);

        if (cancelled) return;

        const [eventsLoad, applicationsLoad, membersLoad, newsLoad, projectsLoad] = loads;

        const upcoming = eventsLoad.data
          .filter(isCurrentOrUpcomingEvent)
          .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

        const pending = [...applicationsLoad.data].sort(
          (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
        );

        setFailedSources(loads.filter((load) => !load.ok).map((load) => load.key));
        setUpcomingEvents(upcoming.slice(0, 5));
        setPendingApplications(pending.slice(0, 5));
        setStats({
          upcomingEvents: upcoming.length,
          pendingApplications: applicationsLoad.data.length,
          members: membersLoad.data.length,
          publishedNews: newsLoad.data.filter(isPublishedNews).length,
          activeProjects: projectsLoad.data.filter(
            (project) => project.isActive && isActiveProjectStatus(project.status),
          ).length,
        });
      } catch (error) {
        console.error('Error rendering admin dashboard data:', error);
        if (!cancelled) {
          setFailedSources(['dashboard']);
          setDashboardError(error instanceof Error ? error.message : String(error));
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    };

    loadDashboard();
    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  const hasPartialError = failedSources.length > 0;

  const locale = i18n.language.startsWith('fr') ? 'fr-CA' : 'en-CA';

  const formatShortDate = (value: string) =>
    new Intl.DateTimeFormat(locale, {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    }).format(new Date(value));

  const formatEventDay = (event: Event) => formatEventDateTime(event.date, locale, event.timeZone, { day: '2-digit' });
  const formatEventMonth = (event: Event) => formatEventDateTime(event.date, locale, event.timeZone, { month: 'short' }).replace('.', '');
  const formatEventTime = (event: Event) => formatEventDateTime(event.date, locale, event.timeZone, { hour: '2-digit', minute: '2-digit' });

  const statCards: { key: string; name: string; value: number; link: string; sub?: string }[] = [
    {
      key: 'upcomingEvents',
      name: t('admin.dashboard.stats.upcomingEvents'),
      value: stats.upcomingEvents,
      link: '/admin/events',
      sub: !isLoading && upcomingEvents[0] ? formatShortDate(upcomingEvents[0].date) : undefined,
    },
    {
      key: 'pendingApplications',
      name: t('admin.dashboard.stats.pendingApplications'),
      value: stats.pendingApplications,
      link: '/admin/membership-applications',
    },
    {
      key: 'members',
      name: t('admin.dashboard.stats.members'),
      value: stats.members,
      link: '/admin/members',
    },
    {
      key: 'publishedNews',
      name: t('admin.dashboard.stats.publishedNews'),
      value: stats.publishedNews,
      link: '/admin/news',
    },
    {
      key: 'activeProjects',
      name: t('admin.dashboard.stats.activeProjects'),
      value: stats.activeProjects,
      link: '/admin/projects',
    },
  ];

  const statIcons: Record<string, string> = {
    upcomingEvents: 'ri-calendar-event-line',
    pendingApplications: 'ri-user-follow-line',
    members: 'ri-group-line',
    publishedNews: 'ri-newspaper-line',
    activeProjects: 'ri-briefcase-4-line',
  };

  const quickActions = [
    { label: t('admin.dashboard.createEvent'), href: '/admin/events/create', icon: 'ri-calendar-event-line' },
    { label: t('admin.dashboard.createNews'), href: '/admin/news/create', icon: 'ri-article-line' },
    { label: t('admin.dashboard.createProject'), href: '/admin/projects/create', icon: 'ri-hammer-line' },
    { label: t('admin.dashboard.createDocument'), href: '/admin/documents/create', icon: 'ri-file-add-line' },
    { label: t('admin.dashboard.reviewApplications'), href: '/admin/membership-applications', icon: 'ri-user-follow-line' },
    { label: t('admin.dashboard.createAssociation'), href: '/admin/associations/create', icon: 'ri-community-line' },
    { label: t('admin.dashboard.createConsultation'), href: '/admin/consultations/create', icon: 'ri-discuss-line' },
    { label: t('admin.dashboard.createGrant'), href: '/admin/grants/create', icon: 'ri-hand-coin-line' },
    { label: t('admin.dashboard.createMember'), href: '/admin/members/create', icon: 'ri-user-add-line' },
  ];

  return (
    <div className="flex flex-col gap-7 xl:h-full xl:min-h-0 xl:gap-4">
      <div className="public-grid-pattern relative overflow-hidden rounded-[22px] bg-green-deep px-6 py-7 text-white shadow-[0_20px_55px_rgba(0,59,27,.13)] sm:px-8 sm:py-8 xl:shrink-0 xl:px-6 xl:py-4">
        <div className="pointer-events-none absolute -right-16 -top-24 h-64 w-64 rounded-full border-[46px] border-white/[0.04]" aria-hidden="true" />
        <div className="relative flex flex-col justify-between gap-6 md:flex-row md:items-end xl:gap-4">
          <div>
            <div className="mb-4 flex items-center gap-3 xl:mb-2">
              <span className="h-0.5 w-9 bg-gold" aria-hidden="true" />
              <p className="text-[10px] font-bold uppercase tracking-[0.18em] text-gold">{t('admin.dashboard.badge')}</p>
            </div>
            <h1 className="font-display text-[34px] font-bold leading-tight text-white sm:text-[42px] xl:text-[30px]">{t('admin.dashboard.title')}</h1>
            <p className="mt-3 max-w-2xl text-[15px] leading-6 text-white/65 xl:mt-1 xl:text-[13px] xl:leading-5">{t('admin.dashboard.subtitle')}</p>
          </div>
          <div className="inline-flex w-fit items-center gap-3 rounded-full border border-white/15 bg-white/[0.07] px-4 py-2.5 text-xs text-white/75 backdrop-blur-sm">
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-gold opacity-40 motion-reduce:animate-none" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-gold" />
            </span>
            {t('admin.dashboard.liveData')}
          </div>
        </div>
      </div>

      {hasPartialError && (
        <div className="border-l-2 border-gold bg-surface p-4">
          <p className="text-body-md text-ink">{t('admin.dashboard.partialError')}</p>
          <p className="mt-1 text-body-md text-ink-variant">
            {t('admin.dashboard.partialErrorSources', {
              sources: failedSources.map((source) => t(`admin.dashboard.source.${source}`)).join(', '),
            })}
          </p>
          {dashboardError && <p className="mt-1 font-mono text-body-md text-error">{dashboardError}</p>}
          <button
            type="button"
            onClick={() => setReloadKey((value) => value + 1)}
            className="mt-3 text-label-md uppercase text-gold-ink transition-colors hover:text-green"
          >
            {t('admin.common.tryAgain')}
          </button>
        </div>
      )}

      <div className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-3 xl:shrink-0 xl:grid-cols-5 xl:gap-3">
        {statCards.map((stat, index) => (
          <Link
            key={stat.key}
            to={stat.link}
            className={`group relative min-h-[154px] overflow-hidden rounded-[18px] border border-green/10 bg-surface p-4 shadow-[0_10px_30px_rgba(0,59,27,.055)] transition-all duration-300 hover:-translate-y-1 hover:border-green/25 hover:shadow-[0_20px_45px_rgba(0,59,27,.10)] sm:min-h-[164px] sm:p-5 xl:min-h-[108px] xl:p-3.5 ${index === statCards.length - 1 ? 'col-span-2 sm:col-span-1' : ''}`}
          >
            <div className="flex items-start justify-between gap-3">
              <span className="text-[10px] font-bold tabular-nums text-green/35">{String(index + 1).padStart(2, '0')}</span>
              <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-green/7 text-lg text-green transition-all group-hover:bg-green group-hover:text-gold xl:h-8 xl:w-8 xl:text-base">
                <i className={statIcons[stat.key]} aria-hidden="true" />
              </span>
            </div>
            <div className="mt-1 flex items-end justify-between gap-4 xl:-mt-1">
              <div>
                <p className="font-display text-[38px] font-bold leading-none tabular-nums text-green-deep xl:text-[29px]">{isLoading ? '—' : stat.value}</p>
                <p className="mt-3 max-w-[150px] text-[10px] font-bold uppercase leading-4 tracking-[0.11em] text-ink-variant xl:mt-1.5 xl:text-[9px] xl:leading-3">{stat.name}</p>
              </div>
              <i className="ri-arrow-right-up-line mb-0.5 text-lg text-green/25 transition-all group-hover:-translate-y-0.5 group-hover:translate-x-0.5 group-hover:text-green" aria-hidden="true" />
            </div>
            {stat.sub && <p className="mt-2 text-xs text-ink-variant">{stat.sub}</p>}
            <span className="absolute inset-x-0 bottom-0 h-1 origin-left scale-x-0 bg-gold transition-transform duration-300 group-hover:scale-x-100" />
          </Link>
        ))}
      </div>

      <div className="grid grid-cols-1 gap-6 xl:min-h-0 xl:flex-1 xl:grid-cols-[minmax(0,1.5fr)_minmax(290px,0.5fr)] xl:grid-rows-2 xl:gap-3">
        <section className="overflow-hidden rounded-[18px] border border-green/10 bg-surface shadow-[0_12px_34px_rgba(0,59,27,.05)] xl:col-start-1 xl:row-start-1 xl:flex xl:min-h-0 xl:flex-col">
          <div className="flex flex-col gap-3 border-b border-green/10 px-5 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-6 xl:shrink-0 xl:px-4 xl:py-3">
            <div>
              <div className="flex items-center gap-3">
                <span className="flex h-9 w-9 items-center justify-center rounded-full bg-gold/20 text-green">
                  <i className="ri-inbox-archive-line" aria-hidden="true" />
                </span>
                <h2 className="font-display text-headline-md text-green">{t('admin.dashboard.inbox.title')}</h2>
              </div>
              <p className="mt-2 text-body-md text-ink-variant sm:pl-12 xl:mt-0.5 xl:text-xs">{t('admin.dashboard.inbox.hint')}</p>
            </div>
            <ArrowLink to="/admin/membership-applications" tone="goldInk">
              {t('admin.common.viewAll')}
            </ArrowLink>
          </div>

          <div className="xl:min-h-0 xl:flex-1 xl:overflow-auto">
            {isLoading ? (
              <div className="flex min-h-[190px] items-center justify-center xl:h-full xl:min-h-0">
                <div className="h-8 w-8 animate-spin rounded-full border-2 border-line border-t-green" />
              </div>
            ) : pendingApplications.length === 0 ? (
              <div className="flex min-h-[190px] flex-col items-center justify-center px-6 py-9 text-center xl:h-full xl:min-h-0 xl:flex-row xl:gap-4 xl:py-3 xl:text-left">
                <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-green/7 text-xl text-green xl:h-10 xl:w-10">
                  <i className="ri-checkbox-circle-line" aria-hidden="true" />
                </span>
                <div>
                  <p className="mt-4 font-display text-[20px] font-bold text-green xl:mt-0 xl:text-[17px]">{t('admin.dashboard.inbox.empty')}</p>
                  <p className="mt-2 max-w-sm text-sm text-ink-variant xl:mt-0.5 xl:text-xs">{t('admin.dashboard.inbox.hint')}</p>
                </div>
              </div>
            ) : (
              <DataTable
                columns={[
                  { key: 'name', label: t('admin.common.name') },
                  { key: 'date', label: t('admin.common.date') },
                  { key: 'status', label: t('admin.common.status') },
                  { key: 'action', label: t('admin.common.actions'), align: 'right' },
                ]}
              >
                {pendingApplications.map((application) => (
                  <tr key={application.id} className="transition-colors hover:bg-surface-container">
                    <Td className="text-ink">
                      <span className="block text-ink">
                        {application.firstName} {application.lastName}
                      </span>
                      <span className="mt-1 block text-body-md text-ink-variant">
                        {application.email}
                        {application.city || application.province
                          ? ` • ${[application.city, application.province].filter(Boolean).join(', ')}`
                          : ''}
                      </span>
                    </Td>
                    <Td>{formatShortDate(application.createdAt)}</Td>
                    <Td>
                      <StatusChip
                        status={application.status.toLowerCase() as 'pending' | 'approved' | 'rejected'}
                        label={t(`admin.applications.status.${application.status.toLowerCase()}`)}
                      />
                    </Td>
                    <Td align="right">
                      <ArrowLink to={`/admin/membership-applications/${application.id}`} tone="goldInk">
                        {t('admin.dashboard.inbox.review')}
                      </ArrowLink>
                    </Td>
                  </tr>
                ))}
              </DataTable>
            )}
          </div>
        </section>

        <section className="public-grid-pattern overflow-hidden rounded-[18px] bg-green-deep p-5 text-white shadow-[0_16px_40px_rgba(0,59,27,.11)] sm:p-6 xl:col-start-2 xl:row-span-2 xl:row-start-1 xl:min-h-0 xl:p-4">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-gold">{t('admin.dashboard.quickActionsHint')}</p>
              <h2 className="mt-2 font-display text-[25px] font-bold text-white xl:mt-1 xl:text-[21px]">{t('admin.dashboard.quickActions')}</h2>
            </div>
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full border border-white/15 bg-white/5 text-gold">
              <i className="ri-flashlight-line text-xl" aria-hidden="true" />
            </span>
          </div>
          <div className="mt-5 space-y-2 xl:mt-3 xl:space-y-1">
            {quickActions.map((action) => (
              <Link key={action.href} to={action.href} className="group flex min-h-[48px] items-center gap-3 rounded-xl border border-white/10 bg-white/[0.045] px-3.5 text-sm font-semibold text-white/85 transition-all hover:border-gold/35 hover:bg-white/[0.09] hover:text-white xl:min-h-[36px] xl:rounded-[10px] xl:px-3 xl:text-[11px]">
                <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-white/[0.07] text-gold transition-colors group-hover:bg-gold group-hover:text-green-deep xl:h-6 xl:w-6 xl:rounded-md">
                  <i className={action.icon} aria-hidden="true" />
                </span>
                <span className="flex-1">{action.label}</span>
                <i className="ri-arrow-right-line text-white/35 transition-transform group-hover:translate-x-0.5 group-hover:text-gold" aria-hidden="true" />
              </Link>
            ))}
          </div>
        </section>
      <section className="overflow-hidden rounded-[18px] border border-green/10 bg-surface shadow-[0_12px_34px_rgba(0,59,27,.05)] xl:col-start-1 xl:row-start-2 xl:flex xl:min-h-0 xl:flex-col">
        <div className="flex flex-col gap-3 border-b border-green/10 px-5 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-6 xl:shrink-0 xl:px-4 xl:py-3">
          <div>
            <div className="flex items-center gap-3">
              <span className="flex h-9 w-9 items-center justify-center rounded-full bg-green/7 text-green">
                <i className="ri-calendar-check-line" aria-hidden="true" />
              </span>
              <h2 className="font-display text-headline-md text-green">
                {t('admin.dashboard.upcomingEventsTitle')}
              </h2>
            </div>
            <p className="mt-2 text-body-md text-ink-variant sm:pl-12 xl:mt-0.5 xl:text-xs">{t('admin.dashboard.upcomingEventsHint')}</p>
          </div>
          <ArrowLink to="/admin/events" tone="goldInk">
            {t('admin.common.viewAll')}
          </ArrowLink>
        </div>

        <div className="xl:min-h-0 xl:flex-1 xl:overflow-auto">
          {isLoading ? (
            <div className="flex min-h-[180px] items-center justify-center xl:h-full xl:min-h-0">
              <div className="h-8 w-8 animate-spin rounded-full border-2 border-line border-t-green" />
            </div>
          ) : upcomingEvents.length === 0 ? (
            <div className="flex min-h-[180px] flex-col items-center justify-center bg-surface/65 px-6 py-9 text-center xl:h-full xl:min-h-0 xl:flex-row xl:gap-4 xl:py-3 xl:text-left">
              <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-green/7 text-xl text-green xl:h-10 xl:w-10">
                <i className="ri-calendar-todo-line" aria-hidden="true" />
              </span>
              <div className="xl:flex xl:items-center xl:gap-4">
                <p className="mt-4 font-display text-[20px] font-bold text-green xl:mt-0 xl:text-[17px]">{t('admin.dashboard.noUpcomingEvents')}</p>
                <Button to="/admin/events/create" variant="secondary" className="mt-5 xl:mt-0 xl:min-h-[38px] xl:px-4 xl:py-2">
                  {t('admin.dashboard.createFirstEvent')}
                </Button>
              </div>
            </div>
          ) : (
            <div className="divide-y divide-green/10 bg-surface">
              {upcomingEvents.map((event) => (
                <Link
                  key={event.id}
                  to={`/admin/events/${event.id}`}
                  className="group relative grid gap-4 px-4 py-4 transition-colors hover:bg-green/[0.035] sm:grid-cols-[76px_minmax(0,1fr)_auto] sm:items-center sm:px-5 xl:grid-cols-[62px_minmax(0,1fr)_minmax(125px,.42fr)_auto] xl:gap-3 xl:px-4 xl:py-2.5"
                >
                  <span className="relative flex h-[72px] w-[72px] shrink-0 flex-col items-center justify-center overflow-hidden rounded-2xl border border-green/15 bg-surface-container text-center shadow-[0_8px_22px_rgba(0,59,27,.06)] transition-all group-hover:-translate-y-0.5 group-hover:border-gold/55 group-hover:shadow-[0_12px_28px_rgba(0,59,27,.11)] xl:h-[58px] xl:w-[58px] xl:rounded-xl">
                    <span className="absolute inset-x-0 top-0 h-1 bg-gold" aria-hidden="true" />
                    <span className="mt-1 text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{formatEventMonth(event)}</span>
                    <span className="font-display text-[27px] font-bold leading-none tabular-nums text-green-deep xl:text-[23px]">{formatEventDay(event)}</span>
                  </span>

                  <span className="min-w-0">
                    <span className="block truncate font-display text-lg font-bold text-green-deep transition-colors group-hover:text-green xl:text-[16px]">
                      {localized(event.title, event.titleEn, i18n.language)}
                    </span>
                    <span className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-ink-variant xl:mt-1 xl:text-[11px]">
                      <span className="inline-flex items-center gap-1.5 font-semibold tabular-nums text-ink"><i className="ri-time-line text-gold-ink" aria-hidden="true" />{formatEventTime(event)}</span>
                      <span className="inline-flex min-w-0 items-center gap-1.5"><i className="ri-map-pin-2-line shrink-0 text-gold-ink" aria-hidden="true" /><span className="truncate">{localized(event.location, event.locationEn, i18n.language) || t('admin.common.na')}</span></span>
                    </span>
                  </span>

                  <span className="flex flex-wrap items-center gap-2 sm:col-start-2 xl:col-start-auto">
                    <StatusChip status={eventLifecycleChipStatus(event)} label={translateEventLifecycle(event, t)} />
                    <span className="inline-flex items-center gap-1.5 rounded-full border border-line bg-surface px-2.5 py-1 text-[9px] font-bold uppercase tracking-[.1em] text-ink-variant">
                      <i className={event.format === 'Online' ? 'ri-live-line' : event.format === 'Hybrid' ? 'ri-broadcast-line' : 'ri-building-2-line'} aria-hidden="true" />
                      {t(`admin.events.format.${event.format}`)}
                    </span>
                  </span>

                  <span className="absolute right-4 top-4 flex h-9 w-9 items-center justify-center rounded-full border border-green/15 bg-surface text-green shadow-sm transition-all group-hover:border-green group-hover:bg-green group-hover:text-white sm:static sm:row-span-2 xl:row-span-1">
                    <i className="ri-arrow-right-line transition-transform group-hover:translate-x-0.5" aria-hidden="true" />
                    <span className="sr-only">{t('admin.common.view')}</span>
                  </span>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>

      </div>

    </div>
  );
};
