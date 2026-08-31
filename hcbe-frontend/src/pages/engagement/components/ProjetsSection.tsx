import { projectsApi } from '../../../lib/api/projects';
import { resolveMediaUrl } from '../../../lib/api/media-url';
import type { Project } from '../../../lib/api/types';
import { localized } from '../../../lib/i18n/localized';
import { EmptyState, StatusChip, Tag, ArrowLink } from '../../../components/ui';

const statusToneMap: Record<string, 'approved' | 'pending' | 'past'> = {
  'En cours': 'approved',
  Actif: 'approved',
  Planification: 'pending',
  Terminé: 'past',
};

const ProjetsSection = () => {
  const { t, i18n } = useTranslation();
  const [projets, setProjets] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState('all');
  const [typeFilter, setTypeFilter] = useState('all');

  useEffect(() => {
    loadProjects();
  }, []);

  const loadProjects = async () => {
    try {
      setLoading(true);
      const response = await projectsApi.getProjects();
      setProjets(response.data);
    } catch (err) {
      console.error('Error loading projects:', err);
      setError(t('public.engagement.projets.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return '';
    const locale = i18n.language.toLowerCase().startsWith('en') ? 'en-CA' : 'fr-CA';
    return new Date(dateString).toLocaleDateString(locale, {
      year: 'numeric',
      month: '2-digit',
    });
  };

  const statusLabel = (status: string) =>
    t(`public.engagement.projets.status.${status}`, { defaultValue: status });

  const typeLabel = (type: string) =>
    t(`public.engagement.projets.type.${type}`, { defaultValue: type });

  const statuses = useMemo(
    () => Array.from(new Set(projets.map((projet) => projet.status))),
    [projets],
  );

  const types = useMemo(() => Array.from(new Set(projets.map((projet) => projet.type))), [projets]);

  const filteredProjets = useMemo(
    () =>
      projets.filter((projet) => {
        const matchesStatus = statusFilter === 'all' || projet.status === statusFilter;
        const matchesType = typeFilter === 'all' || projet.type === typeFilter;
        return matchesStatus && matchesType;
      }),
    [projets, statusFilter, typeFilter],
  );

  const chipClasses = (active: boolean) =>
    `min-h-[40px] rounded-full border px-4 py-2 text-[10px] font-bold uppercase tracking-[0.1em] transition-colors ${
      active ? 'border-green-deep bg-green-deep text-white' : 'border-green/10 bg-background text-ink-variant hover:border-green hover:text-green'
    }`;

  return (
    <main className="bg-surface-container py-12 md:py-16">
      <div className="container-page">
        <div className="flex flex-wrap gap-x-10 gap-y-6 rounded-[18px] border border-green/10 bg-white p-5 shadow-[0_14px_35px_rgba(0,59,27,.06)]">
          <div>
            <p className="mb-3 text-[10px] font-bold uppercase tracking-[0.14em] text-green">
              {t('public.engagement.projets.filter.statusLabel')}
            </p>
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => setStatusFilter('all')}
                className={chipClasses(statusFilter === 'all')}
              >
                {t('public.engagement.projets.filter.all')}
              </button>
              {statuses.map((status) => (
                <button
                  key={status}
                  type="button"
                  onClick={() => setStatusFilter(status)}
                  className={chipClasses(statusFilter === status)}
                >
                  {statusLabel(status)}
                </button>
              ))}
            </div>
          </div>

          <div>
            <p className="mb-3 text-[10px] font-bold uppercase tracking-[0.14em] text-green">
              {t('public.engagement.projets.filter.typeLabel')}
            </p>
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => setTypeFilter('all')}
                className={chipClasses(typeFilter === 'all')}
              >
                {t('public.engagement.projets.filter.all')}
              </button>
              {types.map((type) => (
                <button
                  key={type}
                  type="button"
                  onClick={() => setTypeFilter(type)}
                  className={chipClasses(typeFilter === type)}
                >
                  {typeLabel(type)}
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="mt-8">
          {loading ? (
            <div className="space-y-5">
              {[1, 2].map((item) => (
                <div
                  key={item}
                  className="grid grid-cols-1 gap-6 rounded-[18px] border border-green/10 bg-white p-6 md:grid-cols-[240px_1fr]"
                >
                  <div className="h-[140px] w-full animate-pulse bg-surface-container" />
                  <div className="space-y-3">
                    <div className="h-4 w-1/3 animate-pulse bg-surface-container" />
                    <div className="h-6 w-2/3 animate-pulse bg-surface-container" />
                    <div className="h-4 w-full animate-pulse bg-surface-container" />
                  </div>
                </div>
              ))}
            </div>
          ) : error ? (
            <EmptyState tone="error" icon="ri-error-warning-line" title={error} />
          ) : filteredProjets.length === 0 ? (
            <EmptyState
              icon="ri-building-4-line"
              title={t('public.engagement.projets.empty.title')}
              description={t('public.engagement.projets.empty.description')}
            />
          ) : (
            <div className="space-y-5">
              {filteredProjets.map((projet) => {
                const title = localized(projet.title, projet.titleEn, i18n.language);
                const description = localized(projet.description, projet.descriptionEn, i18n.language);
                const beneficiaries = localized(
                  projet.beneficiaries,
                  projet.beneficiariesEn,
                  i18n.language,
                );
                const hasProgress = typeof projet.progress === 'number' && !Number.isNaN(projet.progress);

                return (
                  <article
                    key={projet.id}
                    className="group grid grid-cols-1 gap-7 overflow-hidden rounded-[20px] border border-green/10 bg-white p-6 transition-all hover:-translate-y-0.5 hover:shadow-[0_20px_48px_rgba(0,59,27,.11)] md:grid-cols-[240px_1fr]"
                  >
                    <div className="relative flex h-[190px] w-full items-center justify-center overflow-hidden rounded-xl bg-green-deep text-gold md:h-full md:min-h-[240px]">
                      <i className="ri-building-4-line text-4xl" aria-hidden="true" />
                      {projet.imageUrl && (
                        <img
                          src={resolveMediaUrl(projet.imageUrl)}
                          alt=""
                          className="absolute inset-0 h-full w-full object-cover transition-transform duration-500 group-hover:scale-[1.02]"
                          onError={(event) => {
                            event.currentTarget.style.display = 'none';
                          }}
                        />
                      )}
                    </div>
                    <div>
                      <div className="flex flex-wrap items-center gap-3">
                        <StatusChip
                          status={statusToneMap[projet.status] ?? 'draft'}
                          label={statusLabel(projet.status)}
                        />
                        <Tag>{typeLabel(projet.type)}</Tag>
                      </div>
                      <h3 className="mt-3 font-display text-headline-md text-green">{title}</h3>
                      <p className="mt-2 max-w-3xl text-body-md text-ink-variant">{description}</p>

                      <dl className="mt-6 grid grid-cols-2 gap-5 rounded-xl bg-background p-5 md:grid-cols-4">
                        <div>
                          <dt className="text-label-md uppercase text-ink-variant">
                            {t('public.engagement.projets.budget')}
                          </dt>
                          <dd className="mt-1 font-display text-headline-md tabular-nums text-green">
                            {projet.budget}
                          </dd>
                        </div>
                        <div>
                          <dt className="text-label-md uppercase text-ink-variant">
                            {t('public.engagement.projets.raised')}
                          </dt>
                          <dd className="mt-1 font-display text-headline-md tabular-nums text-green">
                            {projet.fundsRaised}
                          </dd>
                        </div>
                        <div>
                          <dt className="text-label-md uppercase text-ink-variant">
                            {t('public.engagement.projets.beneficiaries')}
                          </dt>
                          <dd className="mt-1 font-display text-headline-md tabular-nums text-green">
                            {beneficiaries}
                          </dd>
                        </div>
                        <div>
                          <dt className="text-label-md uppercase text-ink-variant">
                            {t('public.engagement.projets.period')}
                          </dt>
                          <dd className="mt-1 font-display text-headline-md tabular-nums text-green">
                            {formatDate(projet.startDate)} –{' '}
                            {projet.endDate
                              ? formatDate(projet.endDate)
                              : t('public.engagement.projets.ongoing')}
                          </dd>
                        </div>
                      </dl>

                      {hasProgress && (
                        <div className="mt-6 flex items-center gap-4">
                          <div className="h-2 flex-grow overflow-hidden rounded-full bg-green/10">
                            <div className="h-full rounded-full bg-green" style={{ width: `${projet.progress}%` }}></div>
                          </div>
                          <span className="text-label-md tabular-nums text-green">{projet.progress}%</span>
                        </div>
                      )}

                      {projet.partners.length > 0 && (
                        <div className="mt-6">
                          <p className="mb-3 text-label-md uppercase text-ink-variant">
                            {t('public.engagement.projets.partners')}
                          </p>
                          <div className="flex flex-wrap gap-2">
                            {projet.partners.map((partenaire, idx) => (
                              <Tag key={idx}>{partenaire}</Tag>
                            ))}
                          </div>
                        </div>
                      )}

                      <div className="mt-6 flex flex-wrap items-center gap-6">
                        <ArrowLink to={`/contact?type=project-contribution&referenceId=${encodeURIComponent(projet.id)}&label=${encodeURIComponent(title)}`} tone="goldInk">
                          {t('public.engagement.projets.contribute')}
                        </ArrowLink>
                        <ArrowLink to={`/projet/${projet.id}`} tone="red">
                          {t('public.engagement.projets.details')}
                        </ArrowLink>
                      </div>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </main>
  );
};

export default ProjetsSection;
