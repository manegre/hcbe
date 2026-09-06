import Navbar from '../../../components/feature/Navbar';
import Footer from '../../../components/feature/Footer';
import ProjetsSection from '../components/ProjetsSection';
import { ArrowLink, PageHeader } from '../../../components/ui';

const ProjetsPage = () => {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main>

      <PageHeader
        variant="interior"
        title={t('public.engagement.projets.title')}
        description={t('public.engagement.projets.subtitle')}
        actions={
          <ArrowLink to="/engagement/consultations" tone="gold">
            {t('public.engagement.page.cards.consultations.title')}
          </ArrowLink>
        }
      />

      <ProjetsSection />

      </main>
      <Footer />
    </div>
  );
};

export default ProjetsPage;
