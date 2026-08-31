import { Link } from 'react-router-dom';
import { Button, ArrowLink, PageHeader } from '../../../components/ui';

const ActualitesHero = () => {
  const { t } = useTranslation();

  return (
    <PageHeader
      variant="hero"
      title={t('public.news.hero.title')}
      description={t('public.news.hero.subtitle')}
      actions={
        <>
          <Button href="#agenda" variant="primary">
            {t('public.news.hero.cta.events')}
          </Button>
          <ArrowLink to="/actualites/annonces" tone="gold">
            {t('public.news.hero.cta.announcements')}
          </ArrowLink>
        </>
      }
      aside={
        <nav className="overflow-hidden rounded-[20px] border border-white/15 bg-black/15 backdrop-blur-md" aria-label={t('public.news.page.section.title')}>
          {[
            ['01', 'ri-calendar-event-line', 'public.news.page.cards.events.title', '/actualites/evenements'],
            ['02', 'ri-megaphone-line', 'public.news.page.cards.announcements.title', '/actualites/annonces'],
            ['03', 'ri-camera-3-line', 'public.news.page.cards.memories.title', '/actualites/souvenirs'],
          ].map(([number, icon, labelKey, path]) => (
            <Link
              key={path}
              to={path}
              className="group flex min-h-[72px] items-center gap-4 border-b border-white/10 px-5 text-left last:border-b-0 hover:bg-white/10"
            >
              <span className="text-[10px] font-bold tracking-[0.18em] text-gold">{number}</span>
              <i className={`${icon} text-xl text-white/70`} aria-hidden="true" />
              <span className="flex-1 text-sm font-semibold text-white">{t(labelKey)}</span>
              <i className="ri-arrow-right-line text-gold transition-transform group-hover:translate-x-1" aria-hidden="true" />
            </Link>
          ))}
        </nav>
      }
    />
  );
};

export default ActualitesHero;
