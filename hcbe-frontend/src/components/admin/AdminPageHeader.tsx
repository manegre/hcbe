import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

interface AdminPageHeaderProps {
  title: string;
  subtitle?: string;
  icon: string;
  count?: number;
  actions?: ReactNode;
}

export const AdminPageHeader = ({ title, subtitle, icon, count, actions }: AdminPageHeaderProps) => {
  const { t } = useTranslation();

  return (
    <header className="admin-page-banner relative overflow-hidden rounded-[18px] border border-line/55 px-5 py-5 sm:px-6 sm:py-6">
      <div className="pointer-events-none absolute -right-10 -top-16 h-44 w-44 rounded-full border-[30px] border-gold/[0.055]" aria-hidden="true" />
      <div className="pointer-events-none absolute inset-y-0 right-[22%] hidden w-px bg-line/35 xl:block" aria-hidden="true" />

      <div className="relative flex flex-wrap items-center justify-between gap-5">
        <div className="flex min-w-0 items-center gap-4">
          <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-[14px] bg-green text-gold shadow-[0_10px_24px_rgba(0,59,27,.16)]">
            <i className={`${icon} text-xl`} aria-hidden="true" />
          </span>
          <div className="min-w-0">
            <p className="flex items-center gap-2 text-[9px] font-bold uppercase tracking-[0.2em] text-ink-variant/65">
              <span className="h-px w-5 bg-gold" aria-hidden="true" />
              {t('admin.list.workspace')}
            </p>
            <h1 className="mt-1 break-words font-display text-[26px] font-bold leading-[1.08] tracking-[-0.02em] text-green-deep sm:text-[32px]">
              {title}
            </h1>
            {subtitle && <p className="mt-1 max-w-2xl text-sm leading-5 text-ink-variant">{subtitle}</p>}
          </div>
        </div>

        <div className="flex items-center gap-3">
          {typeof count === 'number' && (
            <div className="hidden min-w-[108px] rounded-xl border border-line/55 bg-surface/70 px-4 py-2.5 text-right shadow-[0_8px_20px_rgba(0,59,27,.045)] sm:block">
              <p className="font-display text-[24px] font-bold leading-none tabular-nums text-green-deep">{count}</p>
              <p className="mt-1 text-[8px] font-bold uppercase tracking-[0.16em] text-ink-variant/65">
                {t('admin.list.records')}
              </p>
            </div>
          )}
          {actions}
        </div>
      </div>
    </header>
  );
};
