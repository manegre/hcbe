import { useNavigate } from 'react-router-dom';
import Navbar from '../../components/feature/Navbar';
import Footer from '../../components/feature/Footer';
import { projectsApi } from '../../lib/api/projects';
import { resolveMediaUrl } from '../../lib/api/media-url';
import type { Project } from '../../lib/api/types';
import { localized } from '../../lib/i18n/localized';
import { ArrowLink, Button, EmptyState, Tag } from '../../components/ui';

const ProjectDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const [project, setProject] = useState<Project | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!id) {
      navigate('/engagement/projets');
      return;
    }
    loadProject(id);
  }, [id, navigate]);

  const loadProject = async (projectId: string) => {
    try {
      setLoading(true);
      setError(false);
      const response = await projectsApi.getProject(projectId);
      setProject(response.data);
    } catch (err) {
      console.error('Error loading project:', err);
      setError(true);
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return '';
    const locale = i18n.language.toLowerCase().startsWith('en') ? 'en-CA' : 'fr-CA';
    return new Date(dateString).toLocaleDateString(locale, {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  const statusLabel = (status: string) =>
    t(`public.engagement.projets.status.${status}`, { defaultValue: status });
  const typeLabel = (type: string) =>
    t(`public.engagement.projets.type.${type}`, { defaultValue: type });

  if (loading) {
    return (
      <div className="min-h-screen bg-background">
        <Navbar />
        <div className="container-page py-16">
          <div className="space-y-4">
            <div className="h-4 w-40 animate-pulse bg-surface-container" />
            <div className="h-10 w-2/3 animate-pulse bg-surface-container" />
            <div className="h-[360px] w-full animate-pulse bg-surface-container" />
            <div className="h-4 w-32 animate-pulse bg-surface-container" />
            <p className="sr-only">{t('public.engagement.projets.loading')}</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  if (error || !project) {
    return (
      <div className="min-h-screen bg-background">
        <Navbar />
        <div className="container-page py-16">
          <EmptyState
            tone={error ? 'error' : 'empty'}
            icon="ri-building-4-line"
            title={t('public.engagement.projets.notFound')}
            action={
              <ArrowLink to="/engagement/projets" tone="red">
                {t('public.engagement.projets.back')}
              </ArrowLink>
            }
          />
        </div>
        <Footer />
      </div>
    );
  }

  const title = localized(project.title, project.titleEn, i18n.language);
  const description = localized(project.description, project.descriptionEn, i18n.language);
  const beneficiaries = localized(project.beneficiaries, project.beneficiariesEn, i18n.language);
  const hasProgress = typeof project.progress === 'number' && !Number.isNaN(project.progress);

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <header className="public-grid-pattern relative overflow-hidden bg-green-deep py-12 md:py-16">
        <div className="pointer-events-none absolute -right-20 -top-24 h-72 w-72 rounded-full border-[48px] border-white/5" aria-hidden="true" />
        <div className="container-page">
          <Link
            to="/engagement/projets"
            className="inline-flex min-h-[44px] items-center gap-2 text-[10px] font-bold uppercase tracking-[0.14em] text-white/70 transition-colors hover:text-gold"
          >
            <i className="ri-arrow-left-line" aria-hidden="true"></i>
            {t('public.engagement.projets.back')}
          </Link>

          <div className="mt-6 flex flex-wrap items-center gap-3">
            <span className="inline-flex items-center rounded-full border border-gold/60 bg-gold/10 px-3 py-1 text-[10px] font-bold uppercase tracking-[0.12em] text-gold">
              <span className="mr-2 h-1.5 w-1.5 rounded-full bg-gold" aria-hidden="true" />
              {statusLabel(project.status)}
            </span>
            <span className="inline-flex items-center rounded-full border border-white/25 bg-white/5 px-3 py-1 text-[11px] font-medium text-white/85">
              {typeLabel(project.type)}
            </span>
          </div>

          <h1 className="mt-5 max-w-4xl font-display text-[36px] font-bold leading-[1.06] tracking-[-0.03em] text-white md:text-[56px]">{title}</h1>
        </div>
      </header>

      <main className="bg-surface-container py-12 md:py-16">
        <div className="container-page">
          {project.imageUrl && (
            <div className="relative flex h-[360px] w-full items-center justify-center overflow-hidden rounded-[20px] bg-green-deep text-gold shadow-[0_22px_55px_rgba(0,59,27,.14)] md:h-[460px]">
              <i className="ri-building-4-line text-5xl" aria-hidden="true" />
              <img
                src={resolveMediaUrl(project.imageUrl)}
                alt=""
                className="absolute inset-0 h-full w-full object-cover"
                onError={(event) => {
                  event.currentTarget.style.display = 'none';
                }}
              />
            </div>
          )}

          <div className="mt-8 grid grid-cols-1 gap-8 lg:grid-cols-3">
            <div className="rounded-[20px] border border-green/10 bg-white p-6 shadow-[0_18px_48px_rgba(0,59,27,.07)] md:p-10 lg:col-span-2">
              <h2 className="border-b border-line pb-4 font-display text-headline-md text-green">
                {t('public.engagement.projets.descriptionTitle')}
              </h2>
              <p className="mt-6 max-w-[65ch] whitespace-pre-line text-[17px] leading-8 text-ink-variant">
                {description}
              </p>

              <h2 className="mt-16 border-b border-line pb-4 font-display text-headline-md text-green">
                {t('public.engagement.projets.keyFigures')}
              </h2>
              <div className="grid grid-cols-2 gap-px overflow-hidden rounded-xl bg-line md:grid-cols-4">
                {project.budget && (
                  <div className="bg-background p-6">
                    <p className="text-label-md uppercase text-ink-variant">
                      {t('public.engagement.projets.budget')}
                    </p>
                    <p className="mt-2 font-display text-headline-md tabular-nums text-green">
                      {project.budget}
                    </p>
                  </div>
                )}
                {project.fundsRaised && (
                  <div className="bg-background p-6">
                    <p className="text-label-md uppercase text-ink-variant">
                      {t('public.engagement.projets.raised')}
                    </p>
                    <p className="mt-2 font-display text-headline-md tabular-nums text-green">
                      {project.fundsRaised}
                    </p>
                  </div>
                )}
                {beneficiaries && (
                  <div className="bg-background p-6">
                    <p className="text-label-md uppercase text-ink-variant">
                      {t('public.engagement.projets.beneficiaries')}
                    </p>
                    <p className="mt-2 font-display text-headline-md tabular-nums text-green">
                      {beneficiaries}
                    </p>
                  </div>
                )}
                {hasProgress && (
                  <div className="bg-background p-6">
                    <p className="text-label-md uppercase text-ink-variant">
                      {t('public.engagement.projets.progress')}
                    </p>
                    <p className="mt-2 font-display text-headline-md tabular-nums text-green">
                      {project.progress}%
                    </p>
                  </div>
                )}
              </div>

              <h2 className="mt-16 border-b border-line pb-4 font-display text-headline-md text-green">
                {t('public.engagement.projets.timeline')}
              </h2>
              <div className="flex flex-wrap items-center gap-6 rounded-xl bg-background p-6">
                <div>
                  <p className="text-label-md uppercase text-ink-variant">
                    {t('public.engagement.projets.start')}
                  </p>
                  <p className="mt-1 text-body-md text-ink">{formatDate(project.startDate)}</p>
                </div>
                <div className="h-px flex-grow bg-line" aria-hidden="true"></div>
                <div>
                  <p className="text-label-md uppercase text-ink-variant">
                    {t('public.engagement.projets.end')}
                  </p>
                  <p className="mt-1 text-body-md text-ink">
                    {project.endDate ? formatDate(project.endDate) : t('public.engagement.projets.ongoing')}
                  </p>
                </div>
              </div>

              {project.partners.length > 0 && (
                <>
                  <h2 className="mt-16 border-b border-line pb-4 font-display text-headline-md text-green">
                    {t('public.engagement.projets.partners')}
                  </h2>
                  <div className="mt-6 flex flex-wrap gap-2">
                    {project.partners.map((partner, idx) => (
                      <Tag key={idx}>{partner}</Tag>
                    ))}
                  </div>
                </>
              )}
            </div>

            <aside className="lg:sticky lg:top-24 lg:h-fit">
              <div className="rounded-[18px] border border-green/10 bg-white p-6 shadow-[0_14px_35px_rgba(0,59,27,.07)]">
                <h2 className="font-display text-headline-md text-green">
                  {t('public.engagement.projets.progress')}
                </h2>

                {hasProgress && (
                  <div className="mt-4">
                    <div className="h-2 overflow-hidden rounded-full bg-green/10">
                      <div className="h-full rounded-full bg-green" style={{ width: `${project.progress}%` }}></div>
                    </div>
                    <p className="mt-2 text-label-md tabular-nums text-green">{project.progress}%</p>
                  </div>
                )}

                {(project.fundsRaised || project.budget) && (
                  <dl className="mt-6 space-y-3 border-t border-line pt-6">
                    {project.fundsRaised && (
                      <div className="flex items-baseline justify-between gap-4">
                        <dt className="text-label-md uppercase text-ink-variant">
                          {t('public.engagement.projets.raised')}
                        </dt>
                        <dd className="text-right text-body-md tabular-nums text-green">
                          {project.fundsRaised}
                        </dd>
                      </div>
                    )}
                    {project.budget && (
                      <div className="flex items-baseline justify-between gap-4">
                        <dt className="text-label-md uppercase text-ink-variant">
                          {t('public.engagement.projets.budget')}
                        </dt>
                        <dd className="text-right text-body-md tabular-nums text-ink-variant">
                          {project.budget}
                        </dd>
                      </div>
                    )}
                  </dl>
                )}

                <Button to={`/contact?type=project-contribution&referenceId=${encodeURIComponent(project.id)}&label=${encodeURIComponent(title)}`} variant="primary" className="mt-6 w-full">
                  {t('public.engagement.projets.contributeCta')}
                </Button>
              </div>
            </aside>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
};

export default ProjectDetailPage;
