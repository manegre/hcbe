import Navbar from '../../components/feature/Navbar';
import Footer from '../../components/feature/Footer';
import ServicesHero from './components/ServicesHero';
import { SectionHeading } from '../../components/ui';
import { ServiceNav } from './components/ServiceNav';

const ServicesPage = () => {
  const { t } = useTranslation();

  const destinations = [
    {
      id: 1,
      titleKey: 'public.services.page.cards.documents.title',
      descriptionKey: 'public.services.page.cards.documents.description',
      path: '/services/documents-officiels',
      icon: 'ri-file-text-line',
      number: '01',
    },
    {
      id: 2,
      titleKey: 'public.services.page.cards.comites.title',
      descriptionKey: 'public.services.page.cards.comites.description',
      path: '/services/comites',
      icon: 'ri-team-line',
      number: '02',
    },
    {
      id: 3,
      titleKey: 'public.services.page.cards.bourses.title',
      descriptionKey: 'public.services.page.cards.bourses.description',
      path: '/services/bourses',
      icon: 'ri-hand-coin-line',
      number: '03',
    },
  ];

  return (
    <div className="min-h-screen bg-white">
      <Navbar />
      <ServicesHero />
      <ServiceNav />

      <section className="bg-background pb-24 pt-12">
        <div className="container-page">
          <SectionHeading
            title={t('public.services.page.section.title')}
            description={t('public.services.page.section.subtitle')}
          />

          <div className="grid gap-5 lg:grid-cols-3">
            {destinations.map((destination) => (
              <Link
                key={destination.id}
                to={destination.path}
                className="group relative flex min-h-[330px] flex-col overflow-hidden rounded-[20px] border border-green/10 bg-white p-7 shadow-[0_12px_38px_rgba(0,59,27,.06)] transition-all duration-300 hover:-translate-y-1 hover:border-green/25 hover:shadow-[0_22px_50px_rgba(0,59,27,.12)]"
              >
                <span className="absolute right-5 top-2 font-display text-[72px] font-bold leading-none text-green/[0.035]" aria-hidden="true">{destination.number}</span>
                <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-green-deep text-2xl text-gold shadow-[0_8px_20px_rgba(0,59,27,.15)]">
                  <i className={destination.icon} aria-hidden="true" />
                </span>
                <p className="mt-7 text-[10px] font-bold uppercase tracking-[0.16em] text-green/55">Service {destination.number}</p>
                <h3 className="mt-2 font-display text-[25px] font-bold leading-tight text-green-deep">{t(destination.titleKey)}</h3>
                <p className="mt-4 text-[15px] leading-6 text-ink-variant">{t(destination.descriptionKey)}</p>
                <span className="mt-auto flex items-center gap-2 pt-7 text-[11px] font-bold uppercase tracking-[0.12em] text-red-link">
                  {t('public.common.discover')}
                  <i className="ri-arrow-right-line transition-transform group-hover:translate-x-1" aria-hidden="true" />
                </span>
                <span className="absolute inset-x-0 bottom-0 h-1 origin-left scale-x-0 bg-gold transition-transform duration-300 group-hover:scale-x-100" />
              </Link>
            ))}
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
};

export default ServicesPage;
