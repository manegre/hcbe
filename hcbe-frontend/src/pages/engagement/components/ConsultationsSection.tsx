import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { consultationsApi } from '../../../lib/api/consultations';
import type { Consultation } from '../../../lib/api/types';
import { localized, localizedOptional } from '../../../lib/i18n/localized';
import { ArrowLink, Button, EmptyState, SectionHeading, StatusChip } from '../../../components/ui';

const isExternalUrl = (url: string) => /^https?:\/\//i.test(url);

const primaryActionClasses =
  'inline-flex min-h-[44px] items-center justify-center gap-2 rounded-control border border-transparent bg-gold px-6 py-3 text-label-md uppercase text-green transition-colors hover:bg-gold-dim';

const secondaryActionClasses =
  'inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase text-gold-ink transition-colors hover:text-green';

const PrimaryActionLink = ({ url, label }: { url: string; label: string }) =>
  isExternalUrl(url) ? (
    <a href={url} target="_blank" rel="noopener noreferrer" className={primaryActionClasses}>
      {label}
    </a>
  ) : (
    <Link to={url} className={primaryActionClasses}>
      {label}
    </Link>
  );

const SecondaryActionLink = ({ url, label }: { url: string; label: string }) =>
  isExternalUrl(url) ? (
    <a href={url} target="_blank" rel="noopener noreferrer" className={secondaryActionClasses}>
      {label}
      <i className="ri-arrow-right-line text-base" aria-hidden="true"></i>
    </a>
  ) : (
    <ArrowLink to={url} tone="goldInk">
      {label}
    </ArrowLink>
  );

const ConsultationsSection = () => {
  const { t, i18n } = useTranslation();
  const [consultations, setConsultations] = useState<Consultation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    const loadConsultations = async () => {
      try {
        const response = await consultationsApi.getActiveConsultations();
        if (response.success && response.data) {
          setConsultations(response.data);
        } else {
          setError(true);
        }
      } catch (err) {
        console.error('Error loading consultations:', err);
        setError(true);
      } finally {
        setLoading(false);
      }
    };

    loadConsultations();
  }, []);

  const featured = consultations.find((item) => item.layoutType === 'featured');
  const cards = consultations.filter((item) => item.layoutType === 'card');
  const participationUrl = (item: Consultation) => item.governanceType === 'Information'
    ? (item.actionUrl || `/contact?type=consultation-response&referenceId=${encodeURIComponent(item.id)}&label=${encodeURIComponent(localized(item.title, item.titleEn, i18n.language))}`)
    : `/engagement/consultations/${item.id}`;
  const statusLabel = (item: Consultation) => t(`public.engagement.consultations.detail.statusValue.${item.governance?.status || (item.isActive ? 'Open' : 'Draft')}`);

  return (
    <section className="bg-surface-container py-12 md:py-16">
      <div className="container-page">
        <SectionHeading
          title={t('public.engagement.consultations.sectionTitle')}
          description={t('public.engagement.consultations.sectionSubtitle')}
        />

        {loading && (
          <div className="space-y-6">
            {[1, 2, 3].map((item) => (
              <div key={item} className="space-y-4 rounded-[18px] border border-green/10 bg-white p-8">
                <div className="h-5 w-1/3 animate-pulse bg-surface-container" />
                <div className="h-4 w-full animate-pulse bg-surface-container" />
                <div className="h-4 w-4/5 animate-pulse bg-surface-container" />
                <div className="h-11 w-40 animate-pulse bg-surface-container" />
              </div>
            ))}
          </div>
        )}

        {error && !loading && (
          <EmptyState
            tone="error"
            icon="ri-error-warning-line"
            title={t('public.engagement.consultations.errorLoad')}
          />
        )}

        {!loading && !error && consultations.length === 0 && (
          <EmptyState icon="ri-chat-poll-line" title={t('public.engagement.consultations.empty')} />
        )}

        {!loading && !error && featured && (
          <article className="mb-6 grid grid-cols-1 overflow-hidden rounded-[20px] border border-green/10 shadow-[0_20px_48px_rgba(0,59,27,.10)] lg:grid-cols-[0.85fr_1.15fr]">
            <div className="public-grid-pattern flex flex-col justify-between gap-8 bg-green-deep p-8 text-white md:p-10">
              <span className="flex h-16 w-16 items-center justify-center rounded-full border border-white/20 bg-white/10 text-3xl text-gold">
                <i className={featured.icon} aria-hidden="true"></i>
              </span>
              <h3 className="font-display text-headline-lg text-white">
                {localized(featured.title, featured.titleEn, i18n.language)}
              </h3>
            </div>
            <div className="border-t border-line bg-white p-8 md:border-l md:border-t-0 md:p-10">
              <StatusChip
                status={featured.isActive ? 'approved' : 'past'}
                label={
                  statusLabel(featured)
                }
              />
              <p className="mt-4 max-w-3xl text-body-md text-ink-variant">
                {localized(featured.description, featured.descriptionEn, i18n.language)}
              </p>
              {(featured.isActive || featured.actionUrl || featured.secondaryActionUrl) && (
                <div className="mt-6 flex flex-wrap items-center gap-6">
                  {featured.isActive && (
                    <PrimaryActionLink
                      url={participationUrl(featured)}
                      label={
                        localizedOptional(featured.actionLabel, featured.actionLabelEn, i18n.language) ||
                        t('public.engagement.consultations.participate')
                      }
                    />
                  )}
                  {featured.secondaryActionUrl && (
                    <SecondaryActionLink
                      url={featured.secondaryActionUrl}
                      label={
                        localizedOptional(
                          featured.secondaryActionLabel,
                          featured.secondaryActionLabelEn,
                          i18n.language,
                        ) || t('public.engagement.consultations.viewResults')
                      }
                    />
                  )}
                </div>
              )}
            </div>
          </article>
        )}

        {!loading && !error && cards.length > 0 && (
          <div className="grid gap-5 md:grid-cols-2">
            {cards.map((item) => {
              const title = localized(item.title, item.titleEn, i18n.language);
              const description = localized(item.description, item.descriptionEn, i18n.language);
              const actionLabel = localizedOptional(item.actionLabel, item.actionLabelEn, i18n.language);
              const secondaryActionLabel = localizedOptional(
                item.secondaryActionLabel,
                item.secondaryActionLabelEn,
                i18n.language,
              );

              return (
                <article key={item.id} className="flex h-full flex-col rounded-[18px] border border-green/10 bg-white p-7 transition-all hover:-translate-y-0.5 hover:shadow-[0_18px_45px_rgba(0,59,27,.10)]">
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <span className="flex h-12 w-12 items-center justify-center rounded-full bg-gold text-xl text-green-deep">
                      <i className={item.icon} aria-hidden="true"></i>
                    </span>
                    <StatusChip
                      status={item.isActive ? 'approved' : 'past'}
                      label={
                        statusLabel(item)
                      }
                    />
                  </div>

                  <h3 className="mt-6 font-display text-headline-md text-green">{title}</h3>
                  <p className="mt-3 max-w-3xl text-body-md text-ink-variant">{description}</p>

                  {(item.isActive || item.actionUrl || item.secondaryActionUrl) && (
                    <div className="mt-auto flex flex-wrap items-center gap-6 pt-6">
                      {item.isActive && (
                        <PrimaryActionLink
                          url={participationUrl(item)}
                          label={actionLabel || t('public.engagement.consultations.participate')}
                        />
                      )}
                      {item.secondaryActionUrl && (
                        <SecondaryActionLink
                          url={item.secondaryActionUrl}
                          label={secondaryActionLabel || t('public.engagement.consultations.viewResults')}
                        />
                      )}
                    </div>
                  )}
                </article>
              );
            })}
          </div>
        )}

        <div className="public-grid-pattern mt-12 overflow-hidden rounded-[20px] bg-green-deep px-8 py-10 shadow-[0_20px_48px_rgba(0,59,27,.14)] md:px-10">
          <div className="flex flex-col gap-8 md:flex-row md:items-center md:justify-between">
            <div className="md:max-w-2xl">
              <h3 className="font-display text-headline-md text-white">
                {t('public.engagement.consultations.ctaTitle')}
              </h3>
              <p className="mt-4 text-body-md text-green-dim">
                {t('public.engagement.consultations.ctaSubtitle')}
              </p>
            </div>
            <div className="flex flex-wrap gap-4">
              <Button to="/contact" variant="primary">
                {t('public.engagement.consultations.ctaContact')}
              </Button>
              <Button
                to="/actualites/evenements"
                variant="secondary"
                className="border-white text-white hover:bg-white hover:text-green"
              >
                {t('public.engagement.consultations.ctaEvents')}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default ConsultationsSection;
