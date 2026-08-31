import { useTranslation } from 'react-i18next';

const PublicLanguageSwitcher = () => {
  const { i18n, t } = useTranslation();
  const current = i18n.language.startsWith('fr') ? 'fr' : 'en';

  const toggleLanguage = () => {
    i18n.changeLanguage(current === 'fr' ? 'en' : 'fr');
  };

  return (
    <button
      type="button"
      onClick={toggleLanguage}
      aria-label={t('public.lang')}
      className="flex min-h-[44px] items-center text-label-md transition-colors"
    >
      <span className={current === 'fr' ? 'text-green' : 'text-ink-variant'}>FR</span>
      <span className="text-ink-variant">|</span>
      <span className={current === 'en' ? 'text-green' : 'text-ink-variant'}>EN</span>
    </button>
  );
};

export default PublicLanguageSwitcher;
