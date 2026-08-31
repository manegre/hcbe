import { useTranslation } from 'react-i18next';

const LANGUAGES = [
  { code: 'fr', label: 'FR' },
  { code: 'en', label: 'EN' },
] as const;

interface LanguageSwitcherProps {
  variant?: 'default' | 'onDark';
}

export const LanguageSwitcher = ({ variant = 'default' }: LanguageSwitcherProps) => {
  const { i18n, t } = useTranslation();
  const current = i18n.language.startsWith('fr') ? 'fr' : 'en';
  const isOnDark = variant === 'onDark';

  if (isOnDark) {
    return (
      <div className="inline-flex border border-white/30">
        {LANGUAGES.map((lang) => (
          <button
            key={lang.code}
            type="button"
            onClick={() => i18n.changeLanguage(lang.code)}
            aria-pressed={current === lang.code}
            className={`min-h-[44px] px-4 py-2 text-label-md uppercase transition-colors ${
              current === lang.code ? 'bg-white text-green' : 'text-green-dim hover:text-white'
            }`}
          >
            {lang.label}
          </button>
        ))}
      </div>
    );
  }

  return (
    <div className="flex items-center gap-3">
      <span className="hidden text-[9px] font-bold uppercase tracking-[0.18em] text-ink-variant/70 xl:inline">
        {t('admin.common.language')}
      </span>
      <div className="inline-flex rounded-full border border-line/50 bg-surface-container p-1 shadow-inner shadow-green/5">
        {LANGUAGES.map((lang) => (
          <button
            key={lang.code}
            type="button"
            onClick={() => i18n.changeLanguage(lang.code)}
            aria-pressed={current === lang.code}
            className={`min-h-[34px] min-w-[40px] rounded-full px-3 text-[11px] font-bold uppercase tracking-[0.08em] transition-all ${
              current === lang.code
                ? 'bg-green text-white shadow-[0_4px_12px_rgba(0,59,27,.18)]'
                : 'text-ink-variant hover:bg-surface hover:text-green'
            }`}
          >
            {lang.label}
          </button>
        ))}
      </div>
    </div>
  );
};
