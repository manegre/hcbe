import Navbar from '../../../components/feature/Navbar';
import Footer from '../../../components/feature/Footer';
import ComitesSection from '../components/ComitesSection';
import { PageHeader } from '../../../components/ui';
import { ServiceNav } from '../components/ServiceNav';

const ComitesPage = () => {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <PageHeader
        variant="hero"
        title={t('public.services.comites.title')}
        description={t('public.services.comites.subtitle')}
      />
      <ServiceNav />

      <ComitesSection />

      <Footer />
    </div>
  );
};

export default ComitesPage;
