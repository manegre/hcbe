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
    <div className={`flex flex-col gap-6 ${isDirty ? 'pb-24' : ''}`}>
      <div className="admin-page-header flex flex-wrap items-center justify-between gap-4">
        <div className="min-w-0">
          <AdminBackButton to={backPath} label={backLabel} />
          <h1 className="mt-1 font-display text-headline-lg text-green-deep">{title}</h1>
        </div>
        <div className="flex flex-wrap items-center gap-4">
          {secondaryActions}
          {actions}
        </div>
      </div>

      {languageTabs}

      <div className="grid grid-cols-1 gap-gutter lg:grid-cols-[1fr_320px]">
        <div className="min-w-0 space-y-8 [&>section]:admin-panel [&>section]:p-5 sm:[&>section]:p-7">{main}</div>
        {aside && <div className="flex min-w-0 flex-col gap-6">{aside}</div>}
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
