import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '../ui';
import { AdminBackButton } from './AdminBackButton';

interface AdminFormLayoutProps {
  title: string;
  backPath: string;
  backLabel: string;
  languageTabs?: ReactNode;
  /** Rendered to the left of `actions` in the header cluster — secondary,
   * non-destructive affordances such as a "view live" link. */
  secondaryActions?: ReactNode;
  actions: ReactNode;
  main: ReactNode;
  aside?: ReactNode;
  isDirty?: boolean;
  dirtyLabel?: string;
  onCancel?: () => void;
  onSave?: () => void;
}

/**
 * House gabarit for admin create/edit forms: back link + title + actions,
 * optional bilingual language tabs, a main/aside content grid, and an
 * optional fixed unsaved-changes bar. Purely presentational — callers own
 * all form state, validation and submit logic.
 */
export const AdminFormLayout = ({
  title,
  backPath,
  backLabel,
  languageTabs,
  secondaryActions,
  actions,
  main,
  aside,
  isDirty = false,
  dirtyLabel,
  onCancel,
  onSave,
}: AdminFormLayoutProps) => {
  const { t } = useTranslation();

  return (
    <div className={`admin-form-workspace flex flex-col gap-6 ${isDirty ? 'pb-24' : ''}`}>
      <div className="admin-page-header flex flex-wrap items-center justify-between gap-5">
        <div className="min-w-0 max-w-3xl">
          <AdminBackButton to={backPath} label={backLabel} />
          <p className="mt-4 flex items-center gap-2 text-[9px] font-bold uppercase tracking-[.19em] text-red-link"><span className="h-px w-6 bg-gold" />{t('admin.list.workspace')}</p>
          <h1 className="mt-2 font-display text-[30px] font-bold leading-tight tracking-[-.02em] text-green-deep sm:text-[38px]">{title}</h1>
        </div>
        <div className="flex w-full flex-wrap items-center gap-3 border-t border-line/60 pt-4 sm:w-auto sm:border-0 sm:pt-0">
          {secondaryActions}
          {actions}
        </div>
      </div>

      {languageTabs}

      <div className="grid grid-cols-1 items-start gap-6 xl:grid-cols-[minmax(0,1fr)_340px]">
        <div className="admin-form-main min-w-0 space-y-6">{main}</div>
        {aside && <aside className="admin-form-aside flex min-w-0 flex-col gap-5 xl:sticky xl:top-24">{aside}</aside>}
      </div>

      {isDirty && (
        <div className="fixed inset-x-0 bottom-0 z-40 border-t border-line bg-surface p-4">
          <div className="mx-auto flex max-w-container flex-wrap items-center justify-between gap-4">
            <p className="text-body-md text-ink-variant">{dirtyLabel}</p>
            <div className="flex flex-wrap items-center gap-3">
              <Button type="button" variant="secondary" onClick={onCancel}>
                {t('admin.common.cancel')}
              </Button>
              <Button type="button" variant="primary" onClick={onSave}>
                {t('admin.common.save')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
