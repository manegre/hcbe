import { associationsApi } from '../../../lib/api/associations';
import type { Association } from '../../../lib/api/types';
import { Card, Tag, EmptyState, inputClasses, plainTextFromRichText } from '../../../components/ui';
import { localized, localizedOptional } from '../../../lib/i18n/localized';

const AnnuaireSection = () => {
  const { t, i18n } = useTranslation();
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedProvince, setSelectedProvince] = useState('all');
  const [associations, setAssociations] = useState<Association[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    loadAssociations();
  }, []);

  const loadAssociations = async () => {
    try {
      setIsLoading(true);
      const response = await associationsApi.getAssociations();
      if (response.success && response.data) {
        setAssociations(response.data);
      } else {
        setError(t('public.engagement.annuaire.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading associations:', err);
      setError(t('public.engagement.annuaire.errorLoad'));
    } finally {
      setIsLoading(false);
    }
  };

  const provinces = [
    'all',
    ...Array.from(new Set(associations.map((association) => association.province))).sort(),
  ];

  const filteredAssociations = associations.filter((assoc) => {
    const haystack = [
      assoc.name,
      assoc.nameEn,
      assoc.city,
      assoc.province,
      ...assoc.domains,
      ...(assoc.domainsEn || []),
    ]
      .join(' ')
      .toLowerCase();
    const matchesSearch = haystack.includes(searchTerm.toLowerCase());
    const matchesProvince = selectedProvince === 'all' || assoc.province === selectedProvince;
    return matchesSearch && matchesProvince;
  });

  return (
    <section className="bg-surface-container py-12 md:py-16">
      <div className="container-page">
        <div className="flex flex-col gap-4 rounded-[18px] border border-green/10 bg-white p-5 shadow-[0_14px_35px_rgba(0,59,27,.06)] md:flex-row md:items-center md:justify-between">
          <div className="flex flex-1 flex-col gap-4 md:flex-row md:items-center">
            <div className="relative w-full md:max-w-md">
              <i
                className="ri-search-line pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-ink-variant"
                aria-hidden="true"
              ></i>
              <input
                type="search"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder={t('public.engagement.annuaire.searchPlaceholder')}
                aria-label={t('public.engagement.annuaire.searchPlaceholder')}
                className={`${inputClasses} pl-11`}
              />
            </div>
            <select
              value={selectedProvince}
              onChange={(e) => setSelectedProvince(e.target.value)}
              aria-label={t('public.engagement.annuaire.filterAllProvinces')}
              className={`${inputClasses} md:w-auto`}
            >
              {provinces.map((prov) => (
                <option key={prov} value={prov}>
                  {prov === 'all' ? t('public.engagement.annuaire.filterAllProvinces') : prov}
                </option>
              ))}
            </select>
          </div>

          {!isLoading && !error && (
            <p className="inline-flex w-fit items-center rounded-full bg-green-deep px-4 py-2 text-[10px] font-bold uppercase tracking-[0.12em] tabular-nums text-white">
              {t('public.engagement.annuaire.resultCount', { count: filteredAssociations.length })}
            </p>
          )}
        </div>

        <div className="mt-8">
          {isLoading && (
            <div className="grid grid-cols-1 gap-gutter md:grid-cols-2">
              {[1, 2, 3, 4].map((item) => (
                <div key={item} className="h-64 animate-pulse rounded-[18px] border border-green/10 bg-white" />
              ))}
            </div>
          )}

          {!isLoading && error && (
            <EmptyState tone="error" icon="ri-error-warning-line" title={error} />
          )}

          {!isLoading && !error && filteredAssociations.length === 0 && (
            <EmptyState
              icon="ri-building-line"
              title={t('public.engagement.annuaire.emptyTitle')}
              description={
                selectedProvince === 'all'
                  ? t('public.engagement.annuaire.emptyAll')
                  : t('public.engagement.annuaire.emptyFilter')
              }
            />
          )}

          {!isLoading && !error && filteredAssociations.length > 0 && (
            <div className="grid grid-cols-1 gap-gutter md:grid-cols-2">
              {filteredAssociations.map((assoc) => {
                const name = localized(assoc.name, assoc.nameEn, i18n.language);
                const description = plainTextFromRichText(localizedOptional(assoc.description, assoc.descriptionEn, i18n.language));
                const domains = i18n.language.startsWith('en') && assoc.domainsEn?.length ? assoc.domainsEn : assoc.domains;
                const email = assoc.contact?.trim() || '';
                const phone = assoc.phone?.trim() || '';
                const website = assoc.website?.trim() || '';
                const websiteHref = website
                  ? /^https?:\/\//i.test(website)
                    ? website
                    : `https://${website}`
                  : '';

                return (
                  <Card key={assoc.id} hover="green" className="relative overflow-hidden">
                    <span className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-red via-gold to-green" aria-hidden="true" />
                    <div className="flex items-start gap-4">
                      <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-green-deep text-xl text-gold">
                        <i className="ri-community-line" aria-hidden="true" />
                      </span>
                      <div>
                        <h3 className="font-display text-headline-md leading-tight text-green">{name}</h3>
                        <p className="mt-2 flex items-center gap-2 text-sm text-ink-variant">
                          <i className="ri-map-pin-line text-red-link" aria-hidden="true" />
                          {assoc.city}, {assoc.province}
                        </p>
                      </div>
                    </div>

                    {assoc.president?.trim() && (
                      <p className="mt-2 text-body-md text-ink-variant">
                        {t('public.engagement.annuaire.president')} · {assoc.president}
                      </p>
                    )}

                    {(assoc.foundedYear != null || assoc.memberCount?.trim()) && (
                      <div className="mt-1 flex flex-wrap gap-x-4 text-body-md text-ink-variant">
                        {assoc.foundedYear != null && (
                          <span>{t('public.engagement.annuaire.founded', { year: assoc.foundedYear })}</span>
                        )}
                        {assoc.memberCount?.trim() && (
                          <span>
                            {t('public.engagement.annuaire.members', { count: assoc.memberCount })}
                          </span>
                        )}
                      </div>
                    )}

                    {description && (
                      <p className="mt-4 text-body-md text-ink-variant">{description}</p>
                    )}

                    {domains.length > 0 && (
                      <div className="mt-4 flex flex-wrap gap-2">
                        {domains.map((domaine) => (
                          <Tag key={domaine}>{domaine}</Tag>
                        ))}
                      </div>
                    )}

                    {(email || phone || websiteHref) && (
                      <div className="mt-6 flex flex-wrap items-center gap-x-6 gap-y-3 border-t border-line pt-6">
                        {email && (
                          <a
                            href={`mailto:${email}`}
                            className="inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase text-red-link hover:text-green"
                          >
                            <i className="ri-mail-line" aria-hidden="true"></i>
                            {t('public.engagement.annuaire.contactEmail')}
                          </a>
                        )}
                        {phone && (
                          <a
                            href={`tel:${phone.replace(/[^\d+]/g, '')}`}
                            className="inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase text-red-link hover:text-green"
                          >
                            <i className="ri-phone-line" aria-hidden="true"></i>
                            {t('public.engagement.annuaire.contactPhone')}
                          </a>
                        )}
                        {websiteHref && (
                          <a
                            href={websiteHref}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase text-red-link hover:text-green"
                          >
                            <i className="ri-external-link-line" aria-hidden="true"></i>
                            {t('public.engagement.annuaire.visitWebsite')}
                          </a>
                        )}
                      </div>
                    )}
                  </Card>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </section>
  );
};

export default AnnuaireSection;
