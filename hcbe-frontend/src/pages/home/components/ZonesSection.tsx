import zone1DelegatePhoto from '../../../assets/delegates/zone1-delegate.png';
import zone1DeputyPhoto from '../../../assets/delegates/zone1-deputy.png';
import zone2DelegatePhoto from '../../../assets/delegates/zone2-delegate.png';
import zone2DeputyPhoto from '../../../assets/delegates/zone2-deputy.png';
import { ArrowLink, SectionHeading } from '../../../components/ui';

const ZonesSection = () => {
  const { t, i18n } = useTranslation();
  const isEnglish = i18n.language.startsWith('en');

  const zones = [
    {
      name: 'Zone 1',
      welcomeKey: 'public.home.zones.zone1.welcome',
      delegate: {
        name: 'Mâ Ouédraogo Diallo',
        photo: zone1DelegatePhoto,
      },
      deputy: {
        name: 'Ismaël Ratouissanmda Zeba',
        photo: zone1DeputyPhoto,
      },
      regions: isEnglish
        ? ['Ontario', 'Manitoba', 'Saskatchewan', 'Alberta', 'British Columbia', 'Northwest Territories']
        : ['Ontario', 'Manitoba', 'Saskatchewan', 'Alberta', 'Colombie-Britannique', 'Territoires du Nord'],
    },
    {
      name: 'Zone 2',
      welcomeKey: 'public.home.zones.zone2.welcome',
      delegate: {
        name: 'Aziz Ismaël Daboné',
        photo: zone2DelegatePhoto,
      },
      deputy: {
        name: 'Ahmed Arnaud Dao',
        photo: zone2DeputyPhoto,
      },
      regions: isEnglish
        ? ['Quebec', 'New Brunswick', 'Nova Scotia', 'Prince Edward Island', 'Newfoundland and Labrador']
        : ['Québec', 'Nouveau-Brunswick', 'Nouvelle-Écosse', 'Île-du-Prince-Édouard', 'Terre-Neuve-et-Labrador'],
    },
  ];

  return (
    // `border-t` : les deux sections voisines sont `paper` (#FAFAF9) et
    // `background` (#F8F9FA), deux blancs cassés presque identiques. Sans filet,
    // les 192px de marges cumulées se lisent comme un trou, pas comme une
    // frontière.
    <section className="border-t border-line bg-background py-24">
      <div className="container-page">
        <SectionHeading title={t('public.home.zones.title')} description={t('public.home.zones.subtitle')} />

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
          {zones.map((zone, zoneIndex) => (
            <article
              key={zone.name}
              className="group relative flex min-h-full flex-col overflow-hidden rounded-[24px] border border-green/10 bg-white shadow-[0_18px_55px_rgba(0,59,27,.09)] transition-all duration-500 hover:-translate-y-1 hover:shadow-[0_25px_65px_rgba(0,59,27,.14)]"
            >
              <div className={`public-grid-pattern relative overflow-hidden px-6 py-7 sm:px-8 ${zoneIndex === 0 ? 'bg-green-deep' : 'bg-[#164E36]'}`}>
                <span className="pointer-events-none absolute -right-2 -top-12 font-display text-[170px] font-bold leading-none text-white/[0.045]" aria-hidden="true">
                  {zoneIndex + 1}
                </span>
                <div className="relative flex items-start justify-between gap-4">
                  <div>
                    <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-gold">HCBE Canada</p>
                    <h3 className="mt-2 font-display text-[34px] font-bold leading-none text-white">{zone.name}</h3>
                  </div>
                  <div className="rounded-full border border-white/15 bg-white/10 px-4 py-2 text-xs font-semibold text-white/80 backdrop-blur-sm">
                    {t('public.home.zones.territories', { count: zone.regions.length })}
                  </div>
                </div>

                <div className="relative mt-8 grid grid-cols-1 gap-4 sm:grid-cols-2">
                  {[
                    { role: t('public.home.zones.delegate'), person: zone.delegate },
                    { role: t('public.home.zones.deputy'), person: zone.deputy },
                  ].map(({ role, person }, personIndex) => (
                    <div key={person.name} className="flex items-center gap-4 rounded-2xl border border-white/10 bg-white/[0.07] p-3 backdrop-blur-sm">
                      <div className="relative shrink-0">
                        <img src={person.photo} alt="" className="h-[72px] w-[72px] rounded-xl object-cover object-top" />
                        {personIndex === 0 && <span className="absolute -bottom-1 -right-1 flex h-6 w-6 items-center justify-center rounded-full border-2 border-green-deep bg-gold text-[11px] text-green-deep"><i className="ri-star-fill" aria-hidden="true" /></span>}
                      </div>
                      <div className="min-w-0">
                        <p className="text-[9px] font-bold uppercase tracking-[0.14em] text-gold/90">{role}</p>
                        <p className="mt-1 font-display text-[17px] font-semibold leading-snug text-white">{person.name}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="flex flex-1 flex-col px-6 py-7 sm:px-8">
                <div className="relative pl-8">
                  <i className="ri-double-quotes-l absolute left-0 top-0 text-xl text-gold" aria-hidden="true" />
                  <p className="font-display text-[18px] leading-[1.55] text-ink">{t(zone.welcomeKey)}</p>
                </div>

                <div className="mt-8 border-t border-green/10 pt-6">
                  <div className="mb-4 flex items-center justify-between gap-4">
                    <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-green/65">{t('public.home.zones.regions')}</p>
                    <i className="ri-map-2-line text-xl text-green/40" aria-hidden="true" />
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {zone.regions.map((region, regionIndex) => (
                      <span key={region} className="inline-flex items-center gap-2 rounded-full bg-surface-container px-3.5 py-2 text-sm font-medium text-green-deep">
                        <span className={`h-1.5 w-1.5 rounded-full ${regionIndex === 0 ? 'bg-red' : 'bg-gold'}`} aria-hidden="true" />
                        {region}
                      </span>
                    ))}
                  </div>
                </div>

                <div className="mt-auto pt-7">
                  <ArrowLink to="/contact" tone="red" className="border-t border-green/10 pt-4">
                    {t('public.home.zones.cta')}
                  </ArrowLink>
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
