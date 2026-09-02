import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, ArrowLink, PageHeader, StatBar, Reveal } from '../../../components/ui';
import { HeroCarousel } from '../../../components/feature/HeroCarousel';
import heroPhoto from '../../../assets/hero/hero-1.jpg';
import heroAssemblee from '../../../assets/hero/hero-2-assemblee.jpg';
import heroChambre from '../../../assets/hero/hero-3-chambre.jpg';
import heroHautCommissariat from '../../../assets/hero/hero-4-haut-commissariat.jpg';
import { siteContentApi } from '../../../lib/api/site-content';
import type { StatisticDto } from '../../../lib/api/types';
import { useCmsContent } from '../../../contexts/CmsContentContext';
import { resolveMediaUrl } from '../../../lib/api/media-url';

// Photographies décoratives du carrousel du hero (rendues aria-hidden par HeroCarousel).
const HeroSection = () => {
  const { t } = useTranslation();
  const { getValue } = useCmsContent();
  const [statistics, setStatistics] = useState<StatisticDto[]>([]);

  useEffect(() => {
    const loadStatistics = () => siteContentApi.getStatistics().then((response) => {
      if (response.success && response.data) setStatistics(response.data);
    }).catch(() => undefined);
    void loadStatistics();
    window.addEventListener('hcbe:content-published', loadStatistics);
    return () => window.removeEventListener('hcbe:content-published', loadStatistics);
  }, []);

  const statisticValue = (keys: string[], fallback: string) =>
    statistics.find((item) => keys.includes(item.key))?.value || fallback;
  const cmsImage = (key: string, fallback: string) => {
    const value = getValue(key, fallback);
    return value === fallback ? fallback : resolveMediaUrl(value);
  };
  const heroSlides: { src: string; alt: string }[] = [
    { src: cmsImage('media.home.hero.slide1', heroPhoto), alt: '' },
    { src: cmsImage('media.home.hero.slide2', heroAssemblee), alt: '' },
    { src: cmsImage('media.home.hero.slide3', heroChambre), alt: '' },
    { src: cmsImage('media.home.hero.slide4', heroHautCommissariat), alt: '' },
  ];

  return (
    <>
      <HeroCarousel slides={heroSlides}>
        <PageHeader
          bare
          variant="hero"
          immersive
          align="left"
          title={t('public.home.hero.title')}
          description={t('public.home.hero.subtitle')}
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
