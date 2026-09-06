import Navbar from '../../components/feature/Navbar';
import Footer from '../../components/feature/Footer';
import HeroSection from './components/HeroSection';
import { PartnersMarquee } from '../../components/feature/PartnersMarquee';
import ZonesSection from './components/ZonesSection';
import MissionVisionSection from './components/MissionVisionSection';
import UpcomingEventsSection from './components/UpcomingEventsSection';
import RecentAnnouncementsSection from './components/RecentAnnouncementsSection';
import DocumentsSection from './components/DocumentsSection';
import CTASection from './components/CTASection';
import { Reveal } from '../../components/ui';
import { CommunityAdSlot } from '../../components/feature/CommunityAdSlot';

const HomePage = () => {
  return (
    <div className="min-h-screen bg-background text-ink">
      <Navbar />
      <main>
        <HeroSection />
        <PartnersMarquee />
        <MissionVisionSection />
        <Reveal>
          <ZonesSection />
        </Reveal>
        <Reveal>
          <UpcomingEventsSection />
        </Reveal>
        <Reveal>
          <div className="container-page py-8">
            <CommunityAdSlot placement="Homepage" />
          </div>
        </Reveal>
        <Reveal>
          <RecentAnnouncementsSection />
        </Reveal>
        <Reveal>
          <DocumentsSection />
        </Reveal>
        <Reveal>
          <CTASection />
        </Reveal>
      </main>
      <Footer />
    </div>
  );
};

export default HomePage;
