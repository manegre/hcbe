import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation } from 'react-router-dom';
import { Button, DataTable, EmptyState } from '../ui';
import { AdminPageHeader } from './AdminPageHeader';

interface AdminListPageProps {
  title: string;
  count?: number;
  createLabel?: string;
  createPath?: string;
  toolbar?: ReactNode;
  columns: { key: string; label: string; align?: 'left' | 'right' }[];
  children: ReactNode;
  isEmpty: boolean;
  emptyTitle: string;
  emptyDescription?: string;
  error?: string;
  onRetry?: () => void;
  pagination?: {
    page: number;
    totalPages: number;
    totalItems: number;
    onPageChange: (page: number) => void;
  };
}

export const AdminListPage = ({
  title,
  count,
  createLabel,
  createPath,
  toolbar,
  columns,
  children,
  isEmpty,
  emptyTitle,
  emptyDescription,
  error,
  onRetry,
  pagination,
}: AdminListPageProps) => {
  const { t } = useTranslation();
  const location = useLocation();

  const routeMeta = [
    ['/admin/events', 'ri-calendar-event-line'],
    ['/admin/news', 'ri-article-line'],
    ['/admin/documents', 'ri-file-text-line'],
    ['/admin/associations', 'ri-building-line'],
    ['/admin/projects', 'ri-hammer-line'],
    ['/admin/grants', 'ri-hand-coin-line'],
    ['/admin/consultations', 'ri-chat-poll-line'],
    ['/admin/membership-applications', 'ri-user-add-line'],
    ['/admin/submissions', 'ri-inbox-archive-line'],
    ['/admin/members', 'ri-group-line'],
    ['/admin/team-members', 'ri-team-line'],
    ['/admin/users', 'ri-shield-user-line'],
  ] as const;
  const pageIcon = routeMeta.find(([path]) => location.pathname.startsWith(path))?.[1] ?? 'ri-folders-line';

  return (
    <section className="flex flex-col gap-5">
      <AdminPageHeader
        title={title}
        subtitle={t('admin.list.subtitle')}
        icon={pageIcon}
        count={count}
        actions={createLabel && createPath ? (
          <Button to={createPath} variant="primary" className="rounded-xl shadow-[0_10px_24px_rgba(255,205,0,.16)]">
            <i className="ri-add-line text-base" aria-hidden="true" />
            {createLabel}
          </Button>
        ) : undefined}
      />

      {toolbar && (
        <div className="admin-panel overflow-hidden">
          <div className="flex items-center justify-between border-b border-line/50 bg-surface-container/60 px-5 py-3">
            <div className="flex items-center gap-2">
              <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-green/8 text-green">
                <i className="ri-equalizer-2-line" aria-hidden="true" />
              </span>
              <div>
                <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-green-deep">{t('admin.list.filters')}</p>
                <p className="hidden text-xs text-ink-variant/70 sm:block">{t('admin.list.filtersHint')}</p>
              </div>
            </div>
          </div>
          <div className="flex flex-wrap items-end gap-4 p-4 sm:px-5 [&>div]:min-w-[210px] [&>div]:flex-1 lg:[&>div]:max-w-[290px]">{toolbar}</div>
        </div>
      )}

      {error ? (
        <EmptyState
          tone="error"
          title={error}
          action={onRetry ? <Button variant="secondary" onClick={onRetry}>{t('admin.common.tryAgain')}</Button> : undefined}
        />
      ) : isEmpty ? (
        <EmptyState
          title={emptyTitle}
          description={emptyDescription}
          action={createLabel && createPath ? <Button to={createPath} variant="secondary">{createLabel}</Button> : undefined}
        />
      ) : (
        <div className="space-y-3">
          <DataTable columns={columns}>{children}</DataTable>
          {pagination && pagination.totalPages > 1 && (
            <nav className="admin-panel flex flex-wrap items-center justify-between gap-3 px-4 py-3" aria-label={t('admin.list.pagination')}>
              <p className="text-xs text-ink-variant">
                {t('admin.list.totalItems', { count: pagination.totalItems })}
              </p>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  className="inline-flex h-10 items-center gap-1 rounded-lg border border-line px-3 text-xs font-bold uppercase tracking-wide text-green disabled:opacity-40"
                  disabled={pagination.page <= 1}
                  onClick={() => pagination.onPageChange(pagination.page - 1)}
                >
                  <i className="ri-arrow-left-s-line" aria-hidden="true" />
                  {t('admin.list.previous')}
                </button>
                <span className="min-w-20 text-center text-xs font-semibold text-ink">
                  {pagination.page} / {pagination.totalPages}
                </span>
                <button
                  type="button"
                  className="inline-flex h-10 items-center gap-1 rounded-lg border border-line px-3 text-xs font-bold uppercase tracking-wide text-green disabled:opacity-40"
                  disabled={pagination.page >= pagination.totalPages}
                  onClick={() => pagination.onPageChange(pagination.page + 1)}
                >
                  {t('admin.list.next')}
                  <i className="ri-arrow-right-s-line" aria-hidden="true" />
                </button>
              </div>
            </nav>
          )}
        </div>
      )}
    </section>
  );
};
