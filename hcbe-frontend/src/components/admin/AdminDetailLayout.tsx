import type { ReactNode } from 'react';
import { useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { StatusChip } from '../ui';
import { AdminBackButton } from './AdminBackButton';

type StatusChipStatus = 'pending' | 'approved' | 'published' | 'rejected' | 'draft' | 'past';

interface AdminDetailLayoutProps {
  title: string;
  subtitle?: ReactNode;
  backPath: string;
  backLabel?: string;
  status?: { status: StatusChipStatus; label: string };
  /** Rendered to the left of `actions` in the header cluster — secondary,
   * non-destructive affordances such as a "view member" link. */
  secondaryActions?: ReactNode;
  actions?: ReactNode;
  main: ReactNode;
  aside?: ReactNode;
  icon?: string;
}

/**
 * House gabarit for admin detail/read-only pages: back link + title +
 * optional status chip + actions, then a main/aside content grid. Purely
 * presentational — callers own all data fetching, loading, error and
 * not-found branches (which should render through `EmptyState`).
 */
export const AdminDetailLayout = ({
  title,
  subtitle,
  backPath,
  backLabel,
  status,
  secondaryActions,
  actions,
  main,
  aside,
  icon,
}: AdminDetailLayoutProps) => {
  const location = useLocation();
  const { t } = useTranslation();
  const routeIcons = [
    ['/admin/events', 'ri-calendar-event-line'],
    ['/admin/news', 'ri-article-line'],
    ['/admin/documents', 'ri-file-text-line'],
    ['/admin/associations', 'ri-building-line'],
    ['/admin/projects', 'ri-hammer-line'],
    ['/admin/grants', 'ri-hand-coin-line'],
    ['/admin/consultations', 'ri-chat-poll-line'],
    ['/admin/membership-applications', 'ri-user-add-line'],
    ['/admin/members', 'ri-group-line'],
    ['/admin/team-members', 'ri-team-line'],
  ] as const;
  const pageIcon = icon ?? routeIcons.find(([path]) => location.pathname.startsWith(path))?.[1] ?? 'ri-file-list-3-line';

  return (
    <div className="flex flex-col gap-5">
      <header className="admin-detail-banner relative overflow-hidden rounded-[18px] border border-line/55 px-5 py-5 sm:px-6 sm:py-6">
        <div className="pointer-events-none absolute -right-12 -top-16 h-48 w-48 rounded-full border-[34px] border-gold/[0.06]" aria-hidden="true" />
        <div className="relative">
          <div className="mb-4 flex items-center justify-between gap-4">
            <AdminBackButton to={backPath} label={backLabel} />
            <span className="hidden items-center gap-2 text-[9px] font-bold uppercase tracking-[0.18em] text-ink-variant/55 sm:inline-flex">
              <span className="h-1.5 w-1.5 rounded-full bg-gold" aria-hidden="true" />
              {t('admin.detail.record')}
            </span>
          </div>

          <div className="flex flex-wrap items-end justify-between gap-5">
            <div className="flex min-w-0 flex-1 items-start gap-4">
              <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-[14px] bg-green text-gold shadow-[0_10px_24px_rgba(0,59,27,.16)]">
                <i className={`${pageIcon} text-xl`} aria-hidden="true" />
              </span>
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-3">
                  <h1 className="break-words font-display text-[27px] font-bold leading-[1.08] tracking-[-0.025em] text-green-deep sm:text-[34px]">
                    {title}
                  </h1>
                  {status && <StatusChip status={status.status} label={status.label} />}
                </div>
                {subtitle && <div className="mt-1.5 text-sm leading-5 text-ink-variant">{subtitle}</div>}
                {secondaryActions && <div className="mt-3 flex flex-wrap items-center gap-2">{secondaryActions}</div>}
              </div>
            </div>

            {actions && <div className="admin-detail-actions flex w-full flex-wrap items-center gap-2 sm:w-auto">{actions}</div>}
          </div>
        </div>
      </header>

      <div className={`grid grid-cols-1 gap-5 ${aside ? 'xl:grid-cols-[minmax(0,1fr)_330px]' : ''}`}>
        <article className="admin-detail-content admin-panel min-w-0 overflow-hidden p-5 sm:p-7">{main}</article>
        {aside && (
          <aside className="admin-detail-aside flex min-w-0 flex-col gap-4 xl:sticky xl:top-[96px] xl:self-start">
            {aside}
          </aside>
        )}
      </div>
    </div>
  );
};

export const DetailList = ({ children }: { children: ReactNode }) => (
  <dl className="mt-4 divide-y divide-line/60 overflow-hidden rounded-xl border border-line/55 bg-surface-container/35 px-4 sm:px-5">{children}</dl>
);

export const DetailRow = ({ label, value }: { label: string; value: ReactNode }) => (
  <div className="grid grid-cols-1 gap-1 py-3.5 sm:grid-cols-[minmax(150px,.7fr)_minmax(0,1.3fr)] sm:gap-5">
    <dt className="text-[9px] font-bold uppercase tracking-[0.13em] text-ink-variant/70">{label}</dt>
    <dd className="min-w-0 break-words text-[14px] leading-5 text-ink">{value}</dd>
  </div>
);
