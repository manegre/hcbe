import { useTranslation } from 'react-i18next';
import zone1DelegatePhoto from '../../../assets/delegates/zone1-delegate.webp';
import zone1DeputyPhoto from '../../../assets/delegates/zone1-deputy.webp';
import zone2DelegatePhoto from '../../../assets/delegates/zone2-delegate.webp';
import zone2DeputyPhoto from '../../../assets/delegates/zone2-deputy.webp';
import { ArrowLink } from '../../../components/ui';
import { useCmsContent } from '../../../contexts/CmsContentContext';
import { resolveMediaUrl } from '../../../lib/api/media-url';

const ZonesSection = () => {
  const { t } = useTranslation();
  const { getValue } = useCmsContent();
  const cmsImage = (key: string, fallback: string) => {
    const value = getValue(key, fallback);
    return value === fallback ? fallback : resolveMediaUrl(value);
  };

  const zones = [
    {
      name: 'Zone 1',
      welcomeKey: 'public.home.zones.zone1.welcome',
      delegate: {
        name: t('public.home.zones.zone1.delegateName'),
        photo: cmsImage('media.home.zones.zone1.delegate', zone1DelegatePhoto),
      },
      deputy: {
        name: t('public.home.zones.zone1.deputyName'),
        photo: cmsImage('media.home.zones.zone1.deputy', zone1DeputyPhoto),
      },
      regions: t('public.home.zones.zone1.regions').split('|').map((region) => region.trim()),
    },
    {
      name: 'Zone 2',
      welcomeKey: 'public.home.zones.zone2.welcome',
      delegate: {
        name: t('public.home.zones.zone2.delegateName'),
        photo: cmsImage('media.home.zones.zone2.delegate', zone2DelegatePhoto),
      },
      deputy: {
        name: t('public.home.zones.zone2.deputyName'),
        photo: cmsImage('media.home.zones.zone2.deputy', zone2DeputyPhoto),
      },
      regions: t('public.home.zones.zone2.regions').split('|').map((region) => region.trim()),
    },
  ];

  const territoryCount = zones.reduce((count, zone) => count + zone.regions.length, 0);

  return (
    <section className="border-t border-line bg-surface py-16 sm:py-20 lg:py-24">
      <div className="container-page">
        <div className="grid items-end gap-8 lg:grid-cols-[minmax(0,1fr)_auto] lg:gap-14">
          <div className="max-w-3xl">
            <div className="flex items-center gap-3" aria-hidden="true">
              <span className="h-0.5 w-10 bg-gold" />
              <span className="h-1.5 w-1.5 rounded-full bg-red" />
            </div>
            <h2 className="mt-5 font-display text-[34px] font-bold leading-[1.08] tracking-[-0.025em] text-green sm:text-[44px] lg:text-[52px]">
              {t('public.home.zones.title')}
            </h2>
            <p className="mt-5 max-w-2xl text-[17px] leading-7 text-ink-variant">
              {t('public.home.zones.subtitle')}
            </p>
          </div>

          <dl className="grid grid-cols-2 divide-x divide-line border-y border-line lg:min-w-[330px]">
            <div className="py-4 pr-6">
              <dd className="font-display text-[36px] font-bold leading-none text-green">{String(zones.length).padStart(2, '0')}</dd>
              <dt className="mt-2 text-[9px] font-bold uppercase tracking-[0.15em] text-ink-variant">{t('public.home.stats.zones')}</dt>
            </div>
            <div className="py-4 pl-6">
              <dd className="font-display text-[36px] font-bold leading-none text-green">{territoryCount}</dd>
              <dt className="mt-2 text-[9px] font-bold uppercase tracking-[0.15em] text-ink-variant">{t('public.home.stats.provinces')}</dt>
            </div>
          </dl>
        </div>

        <div className="mt-12 overflow-hidden border border-green/15 bg-background shadow-[0_22px_65px_rgba(0,59,27,.08)] sm:mt-14">
          <div className="public-grid-pattern relative flex flex-col gap-5 overflow-hidden bg-green-deep px-6 py-6 text-white sm:flex-row sm:items-center sm:justify-between sm:px-8 lg:px-10">
            <div className="pointer-events-none absolute -right-12 -top-20 h-56 w-56 rounded-full border-[42px] border-white/[0.04]" aria-hidden="true" />
            <div className="relative flex items-center gap-4">
              <span className="flex h-11 w-11 items-center justify-center border border-gold/50 text-gold" aria-hidden="true">
                <i className="ri-map-2-line text-xl" />
              </span>
              <div>
                <p className="text-[9px] font-bold uppercase tracking-[0.2em] text-gold">HCBE Canada</p>
                <p className="mt-1 font-display text-xl font-bold text-white">{t('public.home.zones.representation')}</p>
              </div>
            </div>
            <p className="relative border-l-2 border-gold pl-4 text-[10px] font-bold uppercase tracking-[0.14em] text-white/65">
              {t('public.home.zones.territories', { count: territoryCount })}
            </p>
          </div>

          {zones.map((zone, zoneIndex) => (
            <article
              key={zone.name}
              className={`grid grid-cols-1 ${zoneIndex > 0 ? 'border-t border-green/15' : ''} lg:grid-cols-12`}
            >
              <header className={`public-grid-pattern relative overflow-hidden px-6 py-7 text-white sm:px-8 lg:col-span-3 lg:min-h-[560px] lg:px-8 lg:py-10 ${zoneIndex === 0 ? 'bg-green-deep' : 'bg-[#164E36]'}`}>
                <span className="pointer-events-none absolute -bottom-14 -right-4 font-display text-[230px] font-bold leading-none text-white/[0.05]" aria-hidden="true">
                  {zoneIndex + 1}
                </span>
                <p className="relative text-[9px] font-bold uppercase tracking-[0.2em] text-gold">HCBE Canada</p>
                <div className="relative mt-4 flex items-end justify-between gap-4 lg:mt-10 lg:block">
                  <div>
                    <span className="block font-display text-[72px] font-bold leading-[0.78] text-white/20" aria-hidden="true">0{zoneIndex + 1}</span>
                    <h3 className="mt-4 font-display text-[38px] font-bold leading-none text-white">{zone.name}</h3>
                  </div>
                  <p className="border-l-2 border-gold pl-3 text-[10px] font-bold uppercase leading-5 tracking-[0.12em] text-white/65 lg:mt-10 lg:max-w-[140px]">
                    {t('public.home.zones.territories', { count: zone.regions.length })}
                  </p>
                </div>
              </header>

              <div className="min-w-0 px-6 py-7 sm:px-8 lg:col-span-9 lg:px-10 lg:py-10">
                <div className="flex items-center gap-4">
                  <p className="shrink-0 text-[9px] font-bold uppercase tracking-[0.18em] text-red-link">{t('public.home.zones.representation')}</p>
                  <span className="h-px flex-1 bg-line" aria-hidden="true" />
                </div>

                <div className="mt-6 grid gap-4 md:grid-cols-2">
                  {[
                    { role: t('public.home.zones.delegate'), person: zone.delegate, isLead: true },
                    { role: t('public.home.zones.deputy'), person: zone.deputy, isLead: false },
                  ].map(({ role, person, isLead }, personIndex) => (
                    <div
                      key={person.name}
                      className="group relative overflow-hidden border border-green/15 bg-surface-container/55 p-4 transition-all duration-300 hover:-translate-y-1 hover:border-green/35 hover:shadow-[0_16px_36px_rgba(0,59,27,.10)] sm:p-5"
                    >
                      <span className={`absolute inset-x-0 top-0 h-1 ${isLead ? 'bg-gold' : 'bg-red'}`} aria-hidden="true" />
                      <span className="pointer-events-none absolute -right-1 -top-5 font-display text-[92px] font-bold leading-none text-green/[0.045]" aria-hidden="true">
                        0{personIndex + 1}
                      </span>

                      <div className="relative flex items-center gap-5">
                        <div className="relative shrink-0">
                          <div className="border border-green/15 bg-background p-1.5">
                            <img src={person.photo} alt="" width="112" height="128" loading="lazy" decoding="async" className="h-28 w-24 object-cover object-top grayscale-[6%] transition-all duration-300 group-hover:scale-[1.025] group-hover:grayscale-0 lg:h-32 lg:w-28" />
                          </div>
                        {isLead && (
                            <span className="absolute -bottom-2 -right-2 flex h-8 w-8 items-center justify-center rounded-full border-[3px] border-surface-container bg-gold text-xs text-green-deep shadow-[0_5px_14px_rgba(0,59,27,.18)]" aria-hidden="true">
                            <i className="ri-star-fill" />
                          </span>
                        )}
                        </div>
                        <div className="min-w-0 py-2">
                          <span className={`inline-flex h-7 items-center px-2.5 text-[8px] font-bold uppercase tracking-[0.14em] ${isLead ? 'bg-gold text-green-deep' : 'bg-red-link text-white'}`}>
                            {role}
                          </span>
                          <p className="mt-4 font-display text-[22px] font-bold leading-[1.08] text-green sm:text-[24px]">{person.name}</p>
                          <p className="mt-3 text-[9px] font-bold uppercase tracking-[0.16em] text-ink-variant">HCBE Canada · {zone.name}</p>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>

                <div className="mt-8 grid gap-8 border-t border-line pt-8 md:grid-cols-2 md:gap-10">
                  <div className="relative pl-8">
                    <i className="ri-double-quotes-l absolute left-0 top-0 text-xl text-gold" aria-hidden="true" />
                    <p className="text-[9px] font-bold uppercase tracking-[0.18em] text-red-link">{t('public.home.zones.welcomeLabel')}</p>
                    <p className="mt-3 font-display text-[18px] font-semibold leading-[1.55] text-ink">{t(zone.welcomeKey)}</p>
                  </div>

                  <div className="md:border-l md:border-line md:pl-8">
                    <p className="text-[9px] font-bold uppercase tracking-[0.18em] text-green-deep">{t('public.home.zones.regions')}</p>
                    <ul className="mt-4 grid gap-x-5 gap-y-2.5 sm:grid-cols-2" aria-label={t('public.home.zones.regions')}>
                      {zone.regions.map((region, regionIndex) => (
                        <li key={region} className="flex items-start gap-2.5 text-[13px] font-medium leading-5 text-ink-variant">
                          <span className={`mt-2 h-1.5 w-1.5 shrink-0 rounded-full ${regionIndex === 0 ? 'bg-red' : 'bg-gold'}`} aria-hidden="true" />
                          {region}
                        </li>
                      ))}
                    </ul>

                    <ArrowLink to="/contact" tone="red" className="mt-6 border-t border-line pt-4">
                      {t('public.home.zones.cta')}
                    </ArrowLink>
                  </div>
                </div>
              </div>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
};

export default ZonesSection;
