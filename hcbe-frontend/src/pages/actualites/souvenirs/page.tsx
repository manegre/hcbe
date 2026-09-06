import Navbar from '../../../components/feature/Navbar';
import Footer from '../../../components/feature/Footer';
import GalerieSection from '../components/GalerieSection';
import { PageHeader } from '../../../components/ui';

const MEDIA_EMAIL = 'media@hcbecanada.org';

export const SouvenirsPage = () => {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main>

      <PageHeader
        variant="hero"
        title={t('public.news.souvenirs.title')}
        description={t('public.news.souvenirs.subtitle')}
      />

      <GalerieSection />

      <section className="bg-surface-container py-12 md:py-16">
        <div className="container-page">
          <div className="public-grid-pattern relative overflow-hidden rounded-[20px] bg-green-deep p-8 shadow-[0_20px_48px_rgba(0,59,27,.14)] md:p-10">
            <span className="mb-5 flex h-11 w-11 items-center justify-center rounded-full bg-gold text-green-deep">
              <i className="ri-camera-lens-line text-xl" aria-hidden="true" />
            </span>
            <div className="md:max-w-2xl">
            <h2 className="font-display text-headline-lg text-white">
              {t('public.news.souvenirs.share.title')}
            </h2>
            <p className="mt-4 text-body-md text-green-dim">
              {t('public.news.souvenirs.share.description')}{' '}
              <a href={`mailto:${MEDIA_EMAIL}`} className="font-semibold text-white underline">
                {MEDIA_EMAIL}
              </a>
              .
            </p>
            </div>
          </div>
        </div>
      </section>

      </main>
      <Footer />
    </div>
  );
};

export default SouvenirsPage;
