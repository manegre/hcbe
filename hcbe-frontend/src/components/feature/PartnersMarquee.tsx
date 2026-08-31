import { useEffect, useState, type ComponentType } from 'react';
import { useTranslation } from 'react-i18next';
import { partnerLogos } from '../brand/PartnerLogos';
import { ArrowLink } from '../ui';
import { partnersApi } from '../../lib/api/partners';
import { resolveMediaUrl } from '../../lib/api/media-url';
import type { PartnerDto } from '../../lib/api/types';

type MarqueeItem = {
  id: string;
  name: string;
  altText: string;
  logoUrl?: string;
  websiteUrl?: string;
  Logo?: ComponentType<{ className?: string }>;
};

export const PartnersMarquee = () => {
  const { t, i18n } = useTranslation();
  const [cmsPartners, setCmsPartners] = useState<PartnerDto[] | null>(null);

  useEffect(() => {
    let active = true;
    void partnersApi.getPublic().then((response) => {
      if (active && response.success && response.data) {
        setCmsPartners(response.data.filter((partner) => partner.isFeatured));
      }
    }).catch(() => {
      // Keep the design-time fallback if the CMS API is temporarily unavailable.
    });
    return () => { active = false; };
  }, []);

  const isEnglish = i18n.language.toLowerCase().startsWith('en');
  const fallbackByName = new Map(partnerLogos.map((partner) => [partner.name, partner.Logo]));
  const items: MarqueeItem[] = cmsPartners === null
    ? partnerLogos.map((partner, index) => ({
        id: `fallback-${index}`,
        name: partner.name,
        altText: partner.name,
        Logo: partner.Logo,
      }))
    : cmsPartners.map((partner) => ({
        id: partner.id,
        name: (isEnglish ? partner.nameEn : partner.name) || partner.name,
        altText: (isEnglish ? partner.altTextEn : partner.altText) || partner.name,
        logoUrl: partner.logoUrl,
        websiteUrl: partner.websiteUrl,
        Logo: fallbackByName.get(partner.name),
      }));

  if (cmsPartners?.length === 0) return null;

  const track = [...items, ...items];

  return (
    <section className="relative overflow-hidden bg-background pb-20 pt-4">
      <div className="container-page mb-7 flex flex-col gap-5 md:flex-row md:items-end md:justify-between">
        <div>
          <div className="mb-3 flex items-center gap-3" aria-hidden="true"><span className="h-0.5 w-9 bg-gold" /><span className="h-1.5 w-1.5 rounded-full bg-red" /></div>
          <h2 className="font-display text-[28px] font-bold text-green-deep">{t('public.home.partners.title')}</h2>
          <p className="mt-2 max-w-2xl text-[15px] text-ink-variant">{t('public.home.partners.subtitle')}</p>
        </div>
        <ArrowLink to="/contact" tone="red" className="shrink-0">
          {t('public.home.partners.cta')}
        </ArrowLink>
      </div>

      <div className="relative flex w-full items-center overflow-hidden border-y border-green/10 bg-white py-7 shadow-[0_8px_30px_rgba(0,59,27,.04)] before:absolute before:inset-y-0 before:left-0 before:z-10 before:w-20 before:bg-gradient-to-r before:from-white before:to-transparent after:absolute after:inset-y-0 after:right-0 after:z-10 after:w-20 after:bg-gradient-to-l after:from-white after:to-transparent dark:bg-surface-container dark:before:from-surface-container dark:after:from-surface-container">
        <div className="marquee-track flex w-max shrink-0 items-center">
          {track.map((item, index) => {
            const isDuplicate = index >= items.length;
            const content = (
              <span className="flex h-11 min-w-[150px] shrink-0 items-center justify-center pr-20 text-ink-variant/65 grayscale transition-all duration-300 group-hover:scale-105 group-hover:text-green group-hover:grayscale-0">
                {item.logoUrl ? (
                  <img src={resolveMediaUrl(item.logoUrl)} alt={isDuplicate ? '' : item.altText} className="max-h-11 max-w-[180px] object-contain" />
                ) : item.Logo ? (
                  <item.Logo />
                ) : (
                  <span className="font-display text-xl font-bold leading-none">{item.name}</span>
                )}
              </span>
            );

            return item.websiteUrl ? (
              <a
                key={`${item.id}-${index}`}
                href={item.websiteUrl}
                target="_blank"
                rel="noreferrer"
                className={`group shrink-0 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-green${isDuplicate ? ' marquee-duplicate' : ''}`}
                aria-hidden={isDuplicate || undefined}
                tabIndex={isDuplicate ? -1 : undefined}
                title={item.name}
              >
                {content}
              </a>
            ) : (
              <span
                key={`${item.id}-${index}`}
                className={`group shrink-0${isDuplicate ? ' marquee-duplicate' : ''}`}
                aria-hidden={isDuplicate || undefined}
                title={item.name}
              >
                {content}
              </span>
            );
          })}
        </div>
      </div>
    </section>
  );
};
