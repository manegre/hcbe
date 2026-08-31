import Navbar from '../../../components/feature/Navbar';
import Footer from '../../../components/feature/Footer';
import ConsultationsSection from '../components/ConsultationsSection';
import { ArrowLink, PageHeader } from '../../../components/ui';

const ConsultationsPage = () => {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <PageHeader
        variant="hero"
        title={t('public.engagement.consultations.title')}
        description={t('public.engagement.consultations.subtitle')}
        actions={
          <ArrowLink to="/engagement/annuaire" tone="gold">
            {t('public.engagement.page.cards.associations.title')}
          </ArrowLink>
        }
      />

      <ConsultationsSection />

      <Footer />
    </div>
  );
};

export default ConsultationsPage;
