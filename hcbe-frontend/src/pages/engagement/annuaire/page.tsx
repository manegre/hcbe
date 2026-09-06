import Navbar from '../../../components/feature/Navbar';
import Footer from '../../../components/feature/Footer';
import AnnuaireSection from '../components/AnnuaireSection';
import { ArrowLink, PageHeader } from '../../../components/ui';

const AnnuairePage = () => {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main>

      <PageHeader
        variant="interior"
        title={t('public.engagement.annuaire.title')}
        description={t('public.engagement.annuaire.subtitle')}
        actions={
          <ArrowLink to="/engagement/projets" tone="gold">
            {t('public.engagement.page.cards.projects.title')}
          </ArrowLink>
        }
      />

      <AnnuaireSection />

      </main>
      <Footer />
    </div>
  );
};

export default AnnuairePage;
