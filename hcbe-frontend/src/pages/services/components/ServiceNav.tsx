import { Link, useLocation } from 'react-router-dom';

const serviceLinks = [
  { path: '/services/documents-officiels', labelKey: 'public.services.page.cards.documents.title', icon: 'ri-file-text-line' },
  { path: '/services/comites', labelKey: 'public.services.page.cards.comites.title', icon: 'ri-team-line' },
  { path: '/services/bourses', labelKey: 'public.services.page.cards.bourses.title', icon: 'ri-hand-coin-line' },
] as const;

export const ServiceNav = () => {
  const { t } = useTranslation();
  const location = useLocation();

  return (
    <div className="relative z-20 bg-background">
      <nav className="container-page -translate-y-5" aria-label={t('public.nav.services')}>
        <div className="grid overflow-hidden rounded-[16px] border border-green/10 bg-white shadow-[0_14px_38px_rgba(0,59,27,.10)] sm:grid-cols-3">
          {serviceLinks.map((link) => {
            const active = location.pathname === link.path;
            return (
              <Link
                key={link.path}
                to={link.path}
                aria-current={active ? 'page' : undefined}
                className={`group relative flex min-h-[68px] items-center gap-3 px-5 text-sm font-semibold transition-colors sm:justify-center ${active ? 'bg-green-deep text-white' : 'text-green-deep hover:bg-green/5'}`}
              >
                <i className={`${link.icon} text-xl ${active ? 'text-gold' : 'text-green/55'}`} aria-hidden="true" />
                <span>{t(link.labelKey)}</span>
                {active && <span className="absolute inset-x-0 bottom-0 h-0.5 bg-gold" aria-hidden="true" />}
              </Link>
            );
          })}
        </div>
      </nav>
    </div>
  );
};
