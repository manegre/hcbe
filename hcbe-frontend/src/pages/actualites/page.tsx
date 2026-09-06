import Navbar from '../../components/feature/Navbar';
import Footer from '../../components/feature/Footer';
import ActualitesHero from './components/ActualitesHero';
import AgendaSection from './components/AgendaSection';
import AnnoncesExemples from './components/AnnoncesExemples';
import GalerieSection from './components/GalerieSection';
import { ArrowLink, SectionHeading } from '../../components/ui';
import { CommunityAdSlot } from '../../components/feature/CommunityAdSlot';

const ActualitesPage = () => {
  const { t } = useTranslation();

  const destinations = [
    {
      id: 1,
      titleKey: 'public.news.page.cards.events.title',
      descriptionKey: 'public.news.page.cards.events.description',
      path: '/actualites/evenements',
      icon: 'ri-calendar-event-line',
    },
    {
      id: 2,
      titleKey: 'public.news.page.cards.announcements.title',
      descriptionKey: 'public.news.page.cards.announcements.description',
      path: '/actualites/annonces',
      icon: 'ri-megaphone-line',
    },
    {
      id: 3,
      titleKey: 'public.news.page.cards.memories.title',
      descriptionKey: 'public.news.page.cards.memories.description',
      path: '/actualites/souvenirs',
      icon: 'ri-camera-3-line',
    },
  ];

  return (
    <div className="min-h-screen bg-white">
      <Navbar />
      <main>
      <ActualitesHero />

      <section className="bg-white py-16 md:py-20">
        <div className="container-page">
          <SectionHeading
            title={t('public.news.page.section.title')}
            description={t('public.news.page.section.subtitle')}
          />

          <div className="mt-10 grid gap-5 md:grid-cols-3">
            {destinations.map((destination, index) => (
              <Link
                key={destination.id}
                to={destination.path}
                className={`group relative flex min-h-[280px] flex-col overflow-hidden rounded-[20px] border p-7 transition-all duration-300 hover:-translate-y-1 hover:shadow-[0_22px_50px_rgba(0,59,27,.12)] ${
                  index === 0
                    ? 'border-green-deep bg-green-deep text-white'
                    : 'border-green/10 bg-background text-ink'
                }`}
              >
                <div className="flex items-center justify-between">
                  <span className={`text-[10px] font-bold tracking-[0.2em] ${index === 0 ? 'text-gold' : 'text-red-link'}`}>
                    0{index + 1}
                  </span>
                  <span className={`flex h-11 w-11 items-center justify-center rounded-full ${index === 0 ? 'bg-white/10 text-gold' : 'bg-green/10 text-green'}`}>
                    <i className={`${destination.icon} text-xl`} aria-hidden="true" />
                  </span>
                </div>
                <div className="mt-auto pt-12">
                  <h3 className={`font-display text-headline-md ${index === 0 ? 'text-white' : 'text-green'}`}>
                    {t(destination.titleKey)}
                  </h3>
                  <p className={`mt-3 text-sm leading-6 ${index === 0 ? 'text-white/70' : 'text-ink-variant'}`}>
                    {t(destination.descriptionKey)}
                  </p>
                  <span className={`mt-6 inline-flex items-center gap-2 text-[11px] font-bold uppercase tracking-[0.12em] ${index === 0 ? 'text-gold' : 'text-red-link'}`}>
                    {t('public.common.discover')}
                    <i className="ri-arrow-right-line transition-transform group-hover:translate-x-1" aria-hidden="true" />
                  </span>
                </div>
              </Link>
            ))}
          </div>
          <CommunityAdSlot placement="News" className="mt-10" />
        </div>
      </section>

      <section id="agenda" className="bg-surface-container py-16 md:py-20">
        <div className="container-page">
          <div>
            <SectionHeading
              title={t('public.news.page.agenda.title')}
              action={
                <ArrowLink to="/actualites/evenements" tone="red">
                  {t('public.news.page.agenda.viewAll')}
                </ArrowLink>
              }
            />
            <AgendaSection />
          </div>

          <div className="mt-16 border-t border-green/15 pt-14">
            <SectionHeading
              title={t('public.news.page.annonces.title')}
              action={
                <ArrowLink to="/actualites/annonces" tone="red">
                  {t('public.news.page.annonces.viewAll')}
                </ArrowLink>
              }
            />
            <AnnoncesExemples selectedCategory="all" />
          </div>
        </div>
      </section>

      <GalerieSection />
      </main>

      <Footer />
    </div>
  );
};

export default ActualitesPage;
