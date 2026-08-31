import { Button, ArrowLink, PageHeader, StatBar, Reveal } from '../../../components/ui';
import { HeroCarousel } from '../../../components/feature/HeroCarousel';
import heroPhoto from '../../../assets/hero/hero-1.jpg';
import heroAssemblee from '../../../assets/hero/hero-2-assemblee.jpg';
import heroChambre from '../../../assets/hero/hero-3-chambre.jpg';
import heroHautCommissariat from '../../../assets/hero/hero-4-haut-commissariat.jpg';
import { siteContentApi } from '../../../lib/api/site-content';
import type { PageSectionDto, StatisticDto } from '../../../lib/api/types';

// Photographies décoratives du carrousel du hero (rendues aria-hidden par HeroCarousel).
const heroSlides: { src: string; alt: string }[] = [
  { src: heroPhoto, alt: '' },
  { src: heroAssemblee, alt: '' },
  { src: heroChambre, alt: '' },
  { src: heroHautCommissariat, alt: '' },
];

const HeroSection = () => {
  const { t, i18n } = useTranslation();
  const [statistics, setStatistics] = useState<StatisticDto[]>([]);
  const [heroContent, setHeroContent] = useState<PageSectionDto | null>(null);

  useEffect(() => {
    siteContentApi.getStatistics().then((response) => {
      if (response.success && response.data) setStatistics(response.data);
    }).catch(() => undefined);
    siteContentApi.getPageSections('home').then((response) => {
      if (response.success && response.data) setHeroContent(response.data.find((item) => item.section === 'hero') || null);
    }).catch(() => undefined);
  }, []);

  const statisticValue = (keys: string[], fallback: string) =>
    statistics.find((item) => keys.includes(item.key))?.value || fallback;
  const english = i18n.language.startsWith('en');
  const cmsTitle = english ? heroContent?.titleEn || heroContent?.title : heroContent?.title;
  const cmsDescription = english ? heroContent?.contentEn || heroContent?.content : heroContent?.content;

  return (
    <>
      <HeroCarousel slides={heroSlides}>
        <PageHeader
          bare
          variant="hero"
          immersive
          align="left"
          title={cmsTitle || t('public.home.hero.title')}
          description={cmsDescription || t('public.home.hero.subtitle')}
          actions={
            <>
              <Button to="/services" variant="primary">
                {t('public.home.hero.cta.services')}
              </Button>
              <ArrowLink to="/espace-membre" tone="gold">
                {t('public.home.hero.cta.member')}
              </ArrowLink>
            </>
          }
        />
      </HeroCarousel>
      <Reveal>
        <StatBar
          items={[
            { value: statisticValue(['provinces', 'provinces_covered'], '11'), label: t('public.home.stats.provinces') },
            { value: statisticValue(['zones', 'zones_covered'], '2'), label: t('public.home.stats.zones') },
            { value: statisticValue(['associations'], '15'), label: t('public.home.stats.associations') },
            {
              value: <i className="ri-verified-badge-line text-gold-ink" aria-hidden="true"></i>,
              label: t('public.home.stats.freeMembership'),
            },
          ]}
        />
      </Reveal>
    </>
  );
};

export default HeroSection;
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
