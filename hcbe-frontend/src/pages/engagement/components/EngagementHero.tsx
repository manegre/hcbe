import { Link } from 'react-router-dom';
import { Button, ArrowLink, PageHeader } from '../../../components/ui';

const EngagementHero = () => {
  const { t } = useTranslation();

  return (
    <PageHeader
      variant="hero"
      title={t('public.engagement.hero.title')}
      description={t('public.engagement.hero.subtitle')}
      actions={
        <>
          <Button to="/espace-membre" variant="primary">
            {t('public.engagement.page.cta.member')}
          </Button>
          <ArrowLink to="/engagement/consultations" tone="gold">
            {t('public.engagement.page.cards.consultations.title')}
          </ArrowLink>
        </>
      }
      aside={
        <div className="overflow-hidden rounded-[20px] border border-white/15 bg-black/15 backdrop-blur-md">
          {[
            ['01', 'ri-community-line', 'public.engagement.page.cards.associations.stats', '/engagement/annuaire'],
            ['02', 'ri-focus-3-line', 'public.engagement.page.cards.projects.stats', '/engagement/projets'],
            ['03', 'ri-chat-poll-line', 'public.engagement.page.cards.consultations.stats', '/engagement/consultations'],
          ].map(([number, icon, labelKey, path]) => (
            <Link key={path} to={path} className="group flex min-h-[78px] items-center gap-4 border-b border-white/10 px-5 last:border-b-0 hover:bg-white/10">
              <span className="text-[10px] font-bold tracking-[0.18em] text-gold">{number}</span>
              <span className="flex h-9 w-9 items-center justify-center rounded-full bg-white/10 text-gold">
                <i className={`${icon} text-lg`} aria-hidden="true" />
              </span>
              <span className="flex-1 text-sm font-semibold text-white">{t(labelKey)}</span>
              <i className="ri-arrow-right-line text-gold transition-transform group-hover:translate-x-1" aria-hidden="true" />
            </Link>
          ))}
        </div>
      }
    />
  );
};

export default EngagementHero;
