import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { grantsApi } from '../../../lib/api/grants';
import type { GrantProgram } from '../../../lib/api/types';
import { localized } from '../../../lib/i18n/localized';
import { Button, SectionHeading, EmptyState, plainTextFromRichText } from '../../../components/ui';

const pickCriteria = (grant: GrantProgram, language: string): string[] => {
  const isEnglish = language.toLowerCase().startsWith('en');
  if (isEnglish && grant.eligibilityCriteriaEn && grant.eligibilityCriteriaEn.length > 0) {
    return grant.eligibilityCriteriaEn;
  }
  return grant.eligibilityCriteria ?? [];
};

const BoursesSection = () => {
  const { t, i18n } = useTranslation();
  const [grants, setGrants] = useState<GrantProgram[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadGrants = async () => {
      try {
        setError(null);
        const response = await grantsApi.getActiveGrants();
        if (response.success && response.data) {
          setGrants(response.data);
        } else {
          setError(t('public.grants.errorLoad'));
        }
      } catch (err) {
        console.error('Error loading grants:', err);
        setError(t('public.grants.errorLoad'));
      } finally {
        setLoading(false);
      }
    };

    loadGrants();
  }, [t]);

  return (
    <section id="grants" className="bg-background pb-24 pt-12">
      <div className="container-page">
        <SectionHeading
          title={t('public.grants.sectionTitle')}
          description={t('public.grants.sectionSubtitle')}
        />

        {loading ? (
          <div className="grid gap-6 lg:grid-cols-2">
            {[1, 2, 3].map((item) => (
              <div key={item} className="space-y-4 border border-line bg-surface p-8">
                <div className="h-5 w-1/3 animate-pulse bg-surface-container" />
                <div className="h-4 w-full animate-pulse bg-surface-container" />
                <div className="h-4 w-4/5 animate-pulse bg-surface-container" />
                <div className="h-11 w-40 animate-pulse bg-surface-container" />
              </div>
            ))}
          </div>
        ) : error ? (
          <EmptyState tone="error" icon="ri-error-warning-line" title={error} />
        ) : grants.length === 0 ? (
          <EmptyState
            icon="ri-hand-coin-line"
            title={t('public.grants.emptyTitle')}
            description={t('public.grants.emptyText')}
          />
        ) : (
          <div className="grid gap-6 lg:grid-cols-2">
            {grants.map((grant) => {
              const title = localized(grant.title, grant.titleEn, i18n.language);
              const description = plainTextFromRichText(localized(grant.description, grant.descriptionEn, i18n.language));
              const amount = localized(grant.amount, grant.amountEn, i18n.language);
              const duration = localized(grant.duration, grant.durationEn, i18n.language);
              const criteria = pickCriteria(grant, i18n.language);

              return (
                <article key={grant.id} className="relative flex flex-col overflow-hidden rounded-[20px] border border-green/10 bg-white p-7 shadow-[0_12px_38px_rgba(0,59,27,.06)] sm:p-8">
                  <span className="mb-5 flex h-11 w-11 items-center justify-center rounded-xl bg-green-deep text-xl text-gold"><i className="ri-hand-coin-line" aria-hidden="true" /></span>
                  <h3 className="font-display text-headline-md text-green-deep">{title}</h3>
                  <p className="mt-4 max-w-3xl text-body-md text-ink-variant">{description}</p>
                  <dl className="mt-6 grid grid-cols-2 gap-3">
                    <div className="rounded-xl bg-surface-container p-4">
                      <dt className="text-label-md uppercase text-ink-variant">{t('public.grants.amount')}</dt>
                      <dd className="font-display text-headline-md tabular-nums text-green">{amount}</dd>
                    </div>
                    <div className="rounded-xl bg-surface-container p-4">
                      <dt className="text-label-md uppercase text-ink-variant">{t('public.grants.duration')}</dt>
                      <dd className="font-display text-headline-md tabular-nums text-green">{duration}</dd>
                    </div>
                  </dl>
                  {criteria.length > 0 && (
                    <>
                      <p className="mt-6 text-label-md uppercase text-ink-variant">
                        {t('public.grants.criteriaTitle')}
                      </p>
                      <ul className="mt-3 space-y-2">
                        {criteria.map((criterion) => (
                          <li key={criterion} className="flex items-start gap-3 text-body-md text-ink-variant">
                            <i className="ri-check-line mt-1 text-green" aria-hidden="true"></i>
                            {criterion}
                          </li>
                        ))}
                      </ul>
                    </>
                  )}
                  {grant.applicationUrl ? (
                    <Button href={grant.applicationUrl} variant="primary" className="mt-8 self-start">
                      {t('public.grants.apply')}
                    </Button>
                  ) : (
                    <Button to={`/contact?type=grant-application&referenceId=${encodeURIComponent(grant.id)}&label=${encodeURIComponent(title)}`} variant="primary" className="mt-8 self-start">
                      {t('public.grants.apply')}
                    </Button>
                  )}
                </article>
              );
            })}
          </div>
        )}

        <div className="public-grid-pattern mt-16 overflow-hidden rounded-[20px] bg-green-deep px-8 py-10 shadow-[0_18px_45px_rgba(0,59,27,.14)] md:px-10">
          <div className="flex flex-col gap-8 md:flex-row md:items-center md:justify-between">
            <div className="md:max-w-2xl">
              <h3 className="font-display text-headline-md text-white">{t('public.grants.helpTitle')}</h3>
              <p className="mt-4 text-body-md text-green-dim">{t('public.grants.helpText')}</p>
            </div>
            <div className="flex flex-wrap gap-4">
              <Button to="/contact" variant="primary">
                {t('public.grants.helpContact')}
              </Button>
              <Button
                to="/services"
                variant="secondary"
                className="border-white text-white hover:bg-white hover:text-green"
              >
                {t('public.grants.helpBack')}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default BoursesSection;
