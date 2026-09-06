import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { communityMarketplaceApi } from '../../lib/api/community-marketplace';
import type { AdvertisingCampaign } from '../../lib/api/types';
import { getApiBaseUrl } from '../../lib/api/base-url';
import { localized, localizedOptional } from '../../lib/i18n/localized';

export function CommunityAdSlot({ placement, className = '' }: { placement: string; className?: string }) {
  const { i18n } = useTranslation(); const fr = i18n.language.startsWith('fr');
  const [ads, setAds] = useState<AdvertisingCampaign[]>([]);
  useEffect(() => { communityMarketplaceApi.getAds(placement, fr ? 'fr' : 'en').then((response) => response.data && setAds(response.data)).catch(() => undefined); }, [placement, fr]);
  if (!ads.length) return null;
  return <aside className={className} aria-label={fr ? 'Publicité communautaire' : 'Community advertising'}><p className="mb-2 text-[8px] font-bold uppercase tracking-[.18em] text-ink-variant">{fr ? 'Contenu commandité · Approuvé par le HCBE' : 'Sponsored content · Approved by HCBE'}</p><div className="grid gap-4 md:grid-cols-3">{ads.map((ad) => <a key={ad.id} href={`${getApiBaseUrl()}/api/community-marketplace/ads/${ad.id}/click`} target="_blank" rel="sponsored noopener noreferrer" className="group grid min-h-32 overflow-hidden rounded-2xl border border-green/10 bg-background shadow-[0_10px_28px_rgba(0,59,27,.055)] transition hover:-translate-y-0.5 hover:border-gold md:grid-cols-[110px_1fr] md:col-span-3"><div className="relative min-h-28 bg-green-deep">{ad.imageUrl ? <img src={ad.imageUrl} alt="" className="absolute inset-0 h-full w-full object-cover" /> : <i className="ri-megaphone-line absolute inset-0 flex items-center justify-center text-3xl text-gold" />}</div><div className="p-5"><p className="text-[9px] font-bold uppercase tracking-[.14em] text-red-link">{ad.advertiserName}</p><h3 className="mt-1 font-display text-xl font-bold text-green-deep">{localized(ad.title, ad.titleEn, i18n.language)}</h3><p className="mt-2 line-clamp-2 text-xs leading-5 text-ink-variant">{localizedOptional(ad.body, ad.bodyEn, i18n.language)}</p><span className="mt-3 inline-flex items-center gap-2 text-[9px] font-bold uppercase tracking-wider text-green">{fr ? 'En savoir plus' : 'Learn more'} <i className="ri-arrow-right-up-line" /></span></div></a>)}</div></aside>;
}
