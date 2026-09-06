import Navbar from '../../components/feature/Navbar';
import Footer from '../../components/feature/Footer';
import EngagementHero from './components/EngagementHero';
import { Button, SectionHeading } from '../../components/ui';

const EngagementPage = () => {
  const { t } = useTranslation();

  const destinations = [
    {
      id: 1,
      titleKey: 'public.engagement.page.cards.associations.title',
      descriptionKey: 'public.engagement.page.cards.associations.description',
      statsKey: 'public.engagement.page.cards.associations.stats',
      featureKeys: [
        'public.engagement.page.cards.associations.features.search',
        'public.engagement.page.cards.associations.features.details',
        'public.engagement.page.cards.associations.features.contacts',
      ],
      path: '/engagement/annuaire',
      icon: 'ri-community-line',
    },
    {
      id: 2,
      titleKey: 'public.engagement.page.cards.projects.title',
      descriptionKey: 'public.engagement.page.cards.projects.description',
      statsKey: 'public.engagement.page.cards.projects.stats',
      featureKeys: [
        'public.engagement.page.cards.projects.features.burkina',
        'public.engagement.page.cards.projects.features.local',
        'public.engagement.page.cards.projects.features.tracking',
      ],
      path: '/engagement/projets',
      icon: 'ri-focus-3-line',
    },
    {
      id: 3,
      titleKey: 'public.engagement.page.cards.consultations.title',
      descriptionKey: 'public.engagement.page.cards.consultations.description',
      statsKey: 'public.engagement.page.cards.consultations.stats',
      featureKeys: [
        'public.engagement.page.cards.consultations.features.surveys',
        'public.engagement.page.cards.consultations.features.public',
        'public.engagement.page.cards.consultations.features.feedback',
      ],
      path: '/engagement/consultations',
      icon: 'ri-chat-poll-line',
    },
  ];

  return (
    <div className="min-h-screen bg-white">
      <Navbar />
      <main>
      <EngagementHero />

      <section className="bg-surface-container py-16 md:py-20">
        <div className="container-page">
          <SectionHeading
            title={t('public.engagement.page.section.title')}
            description={t('public.engagement.page.section.subtitle')}
          />

          <div className="mt-10 space-y-5">
            {destinations.map((destination, index) => (
              <Link
                key={destination.id}
                to={destination.path}
                className={`group grid gap-6 overflow-hidden rounded-[20px] border p-6 transition-all hover:-translate-y-0.5 hover:shadow-[0_20px_48px_rgba(0,59,27,.11)] md:grid-cols-[72px_1fr] md:p-8 lg:grid-cols-[72px_1fr_240px] lg:items-center ${
                  index === 0
                    ? 'border-green-deep bg-green-deep text-white'
                    : 'border-green/10 bg-white text-ink'
                }`}
              >
                <div className={`flex h-14 w-14 items-center justify-center rounded-full ${index === 0 ? 'bg-white/10 text-gold' : 'bg-green/10 text-green'}`}>
                  <i className={`${destination.icon} text-2xl`} aria-hidden="true" />
                </div>

                <div>
                  <span className={`text-[10px] font-bold tracking-[0.2em] ${index === 0 ? 'text-gold' : 'text-red-link'}`}>
                    0{index + 1}
                  </span>
                  <h3 className={`mt-2 font-display text-headline-md ${index === 0 ? 'text-white' : 'text-green'}`}>
                    {t(destination.titleKey)}
                  </h3>
                  <p className={`mt-3 max-w-2xl text-sm leading-6 ${index === 0 ? 'text-white/70' : 'text-ink-variant'}`}>
                    {t(destination.descriptionKey)}
                  </p>
                  <div className="mt-5 flex flex-wrap gap-x-5 gap-y-2">
                    {destination.featureKeys.map((featureKey) => (
                      <span key={featureKey} className={`flex items-center gap-2 text-sm ${index === 0 ? 'text-white/70' : 'text-ink-variant'}`}>
                        <i className={`ri-check-line ${index === 0 ? 'text-gold' : 'text-red-link'}`} aria-hidden="true" />
                        {t(featureKey)}
                      </span>
                    ))}
                  </div>
                </div>

                <div className={`flex items-center justify-between gap-4 border-t pt-5 lg:border-l lg:border-t-0 lg:pl-8 lg:pt-0 ${index === 0 ? 'border-white/15' : 'border-green/10'}`}>
                  <div>
                    <p className={`text-[10px] font-bold uppercase tracking-[0.14em] ${index === 0 ? 'text-white/45' : 'text-ink-variant'}`}>
                      {t('public.engagement.page.cards.explore')}
                    </p>
                    <p className={`mt-2 font-display text-xl font-semibold ${index === 0 ? 'text-white' : 'text-green'}`}>
                      {t(destination.statsKey)}
                    </p>
                  </div>
                  <span className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-full transition-transform group-hover:translate-x-1 ${index === 0 ? 'bg-gold text-green-deep' : 'bg-green-deep text-gold'}`}>
                    <i className="ri-arrow-right-line" aria-hidden="true" />
                  </span>
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-white py-12 md:py-16">
        <div className="container-page">
          <div className="public-grid-pattern flex flex-col gap-8 overflow-hidden rounded-[20px] bg-green-deep p-8 shadow-[0_20px_48px_rgba(0,59,27,.14)] md:flex-row md:items-center md:justify-between md:p-10">
          <div className="md:max-w-2xl">
            <h2 className="font-display text-headline-lg text-white">{t('public.engagement.page.cta.title')}</h2>
            <p className="mt-4 text-body-md text-green-dim">{t('public.engagement.page.cta.subtitle')}</p>
          </div>
          <div className="flex flex-wrap gap-4">
            <Button to="/espace-membre" variant="primary">
              {t('public.engagement.page.cta.member')}
            </Button>
            <Button to="/contact" variant="secondary" className="border-white text-white hover:bg-white hover:text-green">
              {t('public.engagement.page.cta.contact')}
            </Button>
          </div>
          </div>
        </div>
      </section>

      </main>
      <Footer />
    </div>
  );
};

export default EngagementPage;
