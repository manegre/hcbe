import Navbar from '../../../components/feature/Navbar';
import Footer from '../../../components/feature/Footer';
import BoursesSection from '../components/BoursesSection';
import { Button, ArrowLink, PageHeader } from '../../../components/ui';
import { ServiceNav } from '../components/ServiceNav';

const rememberItems = [
  {
    titleKey: 'public.grants.remember.eligibility.title',
    descriptionKey: 'public.grants.remember.eligibility.description',
  },
  {
    titleKey: 'public.grants.remember.support.title',
    descriptionKey: 'public.grants.remember.support.description',
  },
  {
    titleKey: 'public.grants.remember.process.title',
    descriptionKey: 'public.grants.remember.process.description',
  },
] as const;

const BoursesPage = () => {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <PageHeader
        variant="hero"
        title={t('public.grants.heroTitle')}
        description={t('public.grants.heroSubtitle')}
        actions={
          <>
            <Button href="#grants" variant="primary">
              {t('public.grants.cta.view')}
            </Button>
            <ArrowLink to="/contact" tone="gold">
              {t('public.grants.cta.ask')}
            </ArrowLink>
          </>
        }
        aside={
          <div className="overflow-hidden rounded-[16px] border border-white/15 bg-white/[0.06] backdrop-blur-sm">
            {rememberItems.map((item, index) => (
              <div key={item.titleKey} className={`p-5 ${index > 0 ? 'border-t border-white/10' : ''}`}>
                <p className="text-label-md uppercase text-gold">{t(item.titleKey)}</p>
                <p className="mt-2 text-body-md text-green-dim">{t(item.descriptionKey)}</p>
              </div>
            ))}
          </div>
        }
      />
      <ServiceNav />

      <BoursesSection />

      <Footer />
    </div>
  );
};

export default BoursesPage;
