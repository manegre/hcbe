import { useTranslation } from 'react-i18next';

const LANGUAGES = [
  { code: 'fr', label: 'FR', name: 'Français' },
  { code: 'en', label: 'EN', name: 'English' },
] as const;

interface LanguageSwitcherProps {
  variant?: 'default' | 'onDark';
  compact?: boolean;
  className?: string;
}

export const LanguageSwitcher = ({
  variant = 'default',
  compact = false,
  className = '',
}: LanguageSwitcherProps) => {
  const { i18n, t } = useTranslation();
  const current = i18n.language.startsWith('fr') ? 'fr' : 'en';
  const isOnDark = variant === 'onDark';

  return (
    <div className={`flex items-center ${compact ? '' : 'gap-3'} ${className}`}>
      {!compact && !isOnDark && (
        <span className="hidden text-[9px] font-bold uppercase tracking-[0.18em] text-ink-variant/70 xl:inline">
          {t('admin.common.language')}
        </span>
      )}

      <div
        role="group"
        aria-label={t('admin.common.language')}
        className={`inline-flex h-10 items-center rounded-full border p-1 backdrop-blur-md transition-colors ${
          isOnDark
            ? 'border-white/15 bg-black/10 shadow-[inset_0_1px_0_rgba(255,255,255,.08),0_8px_24px_rgba(0,0,0,.12)]'
            : 'border-green/10 bg-green/[0.045] shadow-[inset_0_1px_0_rgba(255,255,255,.65),0_6px_18px_rgba(0,59,27,.06)]'
        }`}
      >
        {!compact && (
          <>
            <span
              className={`ml-1 hidden h-7 w-7 shrink-0 items-center justify-center rounded-full sm:flex ${
                isOnDark ? 'text-gold/80' : 'text-green/70'
              }`}
              aria-hidden="true"
            >
              <i className="ri-translate-2 text-[15px]" />
            </span>
            <span className={`mx-1 hidden h-4 w-px sm:block ${isOnDark ? 'bg-white/12' : 'bg-green/10'}`} aria-hidden="true" />
          </>
        )}

        <div className="inline-flex items-center gap-0.5">
          {LANGUAGES.map((lang) => (
            <button
              key={lang.code}
              type="button"
              onClick={() => void i18n.changeLanguage(lang.code)}
              aria-label={lang.name}
              title={lang.name}
              aria-pressed={current === lang.code}
              className={`flex h-8 min-w-9 items-center justify-center rounded-full px-2 text-[10px] font-bold uppercase tracking-[0.1em] transition-all duration-200 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-1 sm:min-w-10 sm:px-2.5 ${
                current === lang.code
                  ? 'bg-gold text-green-deep shadow-[0_4px_12px_rgba(255,205,0,.2)] focus-visible:outline-white'
                  : isOnDark
                    ? 'text-white/60 hover:bg-white/[0.07] hover:text-white focus-visible:outline-gold'
                    : 'text-ink-variant hover:bg-surface hover:text-green focus-visible:outline-green'
              }`}
            >
              {lang.label}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
};

export default LanguageSwitcher;
