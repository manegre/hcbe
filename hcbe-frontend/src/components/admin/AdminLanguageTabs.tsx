import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

export type AdminContentLanguage = 'fr' | 'en';

interface AdminLanguageTabsProps {
  frPanel: ReactNode;
  enPanel: ReactNode;
  /** Show a reminder badge on the EN tab when French content exists but EN is empty. */
  enIncomplete?: boolean;
  className?: string;
  defaultLanguage?: AdminContentLanguage;
}

/**
 * Tabs for bilingual CMS fields. Both panels stay mounted (hidden via CSS)
 * so HTML5 `required` on French fields still validates on submit.
 */
export const AdminLanguageTabs = ({
  frPanel,
  enPanel,
  enIncomplete = false,
  className = '',
  defaultLanguage = 'fr',
}: AdminLanguageTabsProps) => {
  const { t } = useTranslation();
  const [active, setActive] = useState<AdminContentLanguage>(defaultLanguage);

  const tabClass = (lang: AdminContentLanguage) =>
    [
      'flex min-h-[44px] items-center gap-2 border-b-[3px] px-1 text-label-md uppercase transition-colors',
      active === lang
        ? 'border-gold text-green'
        : 'border-transparent text-ink-variant hover:text-green',
    ].join(' ');

  return (
    <div className={`min-w-0 ${className}`}>
      <div
        role="tablist"
        aria-label={t('admin.content.lang.tabsLabel')}
        className="mb-5 flex gap-8 overflow-x-auto border-b border-line"
      >
        <button
          type="button"
          role="tab"
          id="admin-lang-tab-fr"
          aria-controls="admin-lang-panel-fr"
          aria-selected={active === 'fr'}
          className={tabClass('fr')}
          onClick={() => setActive('fr')}
        >
          {t('admin.content.lang.tabFr')}
        </button>
        <button
          type="button"
          role="tab"
          id="admin-lang-tab-en"
          aria-controls="admin-lang-panel-en"
          aria-selected={active === 'en'}
          className={tabClass('en')}
          onClick={() => setActive('en')}
        >
          {t('admin.content.lang.tabEn')}
          {enIncomplete && (
            <span
              className="h-2 w-2 bg-gold"
              aria-label={t('admin.content.lang.incompleteBadge')}
              title={t('admin.content.lang.incompleteBadge')}
            ></span>
          )}
        </button>
      </div>

      <p className="mb-4 text-body-md text-ink-variant">
        {active === 'fr' ? t('admin.content.lang.frHint') : t('admin.content.lang.enHint')}
      </p>

      <div
        role="tabpanel"
        id="admin-lang-panel-fr"
        aria-labelledby="admin-lang-tab-fr"
        hidden={active !== 'fr'}
        className={`min-w-0 ${active === 'fr' ? '' : 'hidden'}`}
      >
        {frPanel}
      </div>
      <div
        role="tabpanel"
        id="admin-lang-panel-en"
        aria-labelledby="admin-lang-tab-en"
        hidden={active !== 'en'}
        className={`min-w-0 ${active === 'en' ? '' : 'hidden'}`}
      >
        {enPanel}
      </div>
    </div>
  );
};

/** True when at least one French value is filled and the matching English value is empty. */
export const isEnglishContentIncomplete = (
  pairs: Array<[french?: string | null, english?: string | null]>,
): boolean =>
  pairs.some(([french, english]) => {
    const fr = french?.trim() ?? '';
    const en = english?.trim() ?? '';
    return fr.length > 0 && en.length === 0;
  });
