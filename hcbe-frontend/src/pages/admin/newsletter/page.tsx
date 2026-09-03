import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { newsletterApi } from '../../../lib/api/newsletter';
import type { CreateNewsletterCampaignRequest, NewsletterCampaignDto, NewsletterSubscriptionDto } from '../../../lib/api/types';
import { Button, DataTable, EmptyState, Field, StatusChip, Td, inputClasses } from '../../../components/ui';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { AdminStatCard } from '../../../components/admin/AdminStatCard';

type ActiveFilter = 'all' | 'active' | 'inactive';
type LanguageFilter = 'all' | 'fr' | 'en';

const NewsletterAdminPage: React.FC = () => {
  const { t, i18n } = useTranslation();
  const [subscriptions, setSubscriptions] = useState<NewsletterSubscriptionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [languageFilter, setLanguageFilter] = useState<LanguageFilter>('all');
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>('active');
  const [actionId, setActionId] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);
  const [campaigns, setCampaigns] = useState<NewsletterCampaignDto[]>([]);
  const [campaignBusy, setCampaignBusy] = useState(false);
  const emptyCampaign: CreateNewsletterCampaignRequest = { subject: '', subjectEn: '', body: '', bodyEn: '', audience: 'Newsletter', preferenceCategory: 'newsletter' };
  const [campaignForm, setCampaignForm] = useState<CreateNewsletterCampaignRequest>(emptyCampaign);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  const locale = i18n.language.startsWith('fr') ? 'fr-CA' : 'en-CA';

  const loadSubscriptions = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await newsletterApi.searchSubscriptions({
        page,
        search,
        language: languageFilter === 'all' ? undefined : languageFilter,
        isActive: activeFilter === 'all' ? undefined : activeFilter === 'active',
      });
      if (response.success && response.data) {
        setSubscriptions(response.data.items);
        setTotalItems(response.data.totalItems);
        setTotalPages(response.data.totalPages);
      } else {
        setError(t('admin.newsletter.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading newsletter subscriptions:', err);
      setError(t('admin.newsletter.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSubscriptions();
  }, [languageFilter, activeFilter, page, search]);

  const loadCampaigns = async () => {
    const response = await newsletterApi.getCampaigns();
    if (response.success && response.data) setCampaigns(response.data);
  };

  useEffect(() => {
    loadCampaigns().catch((err) => console.error('Error loading campaigns:', err));
  }, []);

  const handleCreateCampaign = async (event: React.FormEvent) => {
    event.preventDefault();
    setCampaignBusy(true);
    try {
      const response = await newsletterApi.createCampaign(campaignForm);
      if (response.success) {
        setCampaignForm(emptyCampaign);
        await loadCampaigns();
      } else {
        setError(response.message || t('admin.newsletter.campaignError'));
      }
    } finally {
      setCampaignBusy(false);
    }
  };

  const handleSendCampaign = async (id: string) => {
    if (!window.confirm(t('admin.newsletter.confirmSend'))) return;
    setCampaignBusy(true);
    try {
      const response = await newsletterApi.sendCampaign(id);
      if (!response.success) setError(response.message || t('admin.newsletter.campaignError'));
      await loadCampaigns();
    } finally {
      setCampaignBusy(false);
    }
  };

  const handleDeactivate = async (id: string) => {
    if (!window.confirm(t('admin.newsletter.confirmDeactivate'))) return;

    try {
      setActionId(id);
      const response = await newsletterApi.updateActive(id, { isActive: false });
      if (response.success) {
        await loadSubscriptions();
      }
    } catch (err) {
      console.error('Error deactivating subscription:', err);
    } finally {
      setActionId(null);
    }
  };

  const handleReactivate = async (id: string) => {
    try {
      setActionId(id);
      const response = await newsletterApi.updateActive(id, { isActive: true });
      if (response.success) {
        await loadSubscriptions();
      }
    } catch (err) {
      console.error('Error reactivating subscription:', err);
    } finally {
      setActionId(null);
    }
  };

  const handleExport = async () => {
    try {
      setExporting(true);
      const blob = await newsletterApi.exportCsv();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = 'newsletter-subscribers.csv';
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Error exporting newsletter CSV:', err);
      setError(t('admin.newsletter.errorExport'));
    } finally {
      setExporting(false);
    }
  };

  const formatDate = (value: string) =>
    new Intl.DateTimeFormat(locale, {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    }).format(new Date(value));

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  const activeCount = subscriptions.filter((item) => item.isActive).length;
  const inactiveCount = subscriptions.length - activeCount;
  const frenchCount = subscriptions.filter((item) => item.preferredLanguage?.toLowerCase() === 'fr').length;

  const toolbar = (
    <>
      <Field label={t('admin.list.search')} htmlFor="newsletter-search">
        <input id="newsletter-search" className={inputClasses} value={search} placeholder={t('admin.list.searchPlaceholder')} onChange={(event) => { setSearch(event.target.value); setPage(1); }} />
      </Field>
      <Field label={t('admin.common.language')} htmlFor="newsletter-language">
        <select
          id="newsletter-language"
          value={languageFilter}
          onChange={(e) => { setLanguageFilter(e.target.value as LanguageFilter); setPage(1); }}
          className={inputClasses}
        >
          <option value="all">{t('admin.newsletter.filterLanguageAll')}</option>
          <option value="fr">{t('admin.newsletter.filterLanguageFr')}</option>
          <option value="en">{t('admin.newsletter.filterLanguageEn')}</option>
        </select>
      </Field>
      <Field label={t('admin.common.status')} htmlFor="newsletter-status">
        <select
          id="newsletter-status"
          value={activeFilter}
          onChange={(e) => { setActiveFilter(e.target.value as ActiveFilter); setPage(1); }}
          className={inputClasses}
        >
          <option value="all">{t('admin.newsletter.filterStatusAll')}</option>
          <option value="active">{t('admin.newsletter.filterStatusActive')}</option>
          <option value="inactive">{t('admin.newsletter.filterStatusInactive')}</option>
        </select>
      </Field>
    </>
  );

  return (
    <section className="flex flex-col gap-5">
      <AdminPageHeader
        title={t('admin.newsletter.title')}
        subtitle={t('admin.newsletter.subtitle')}
        icon="ri-mail-send-line"
        count={totalItems}
        actions={(
          <Button variant="secondary" onClick={handleExport} disabled={exporting} className="rounded-xl bg-surface/70">
            <i className="ri-download-line" aria-hidden="true"></i>
            {exporting ? t('admin.newsletter.exporting') : t('admin.newsletter.export')}
          </Button>
        )}
      />

      <div className="admin-panel overflow-hidden">
        <div className="flex items-center gap-2 border-b border-line/50 bg-surface-container/60 px-5 py-3">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-green/8 text-green">
            <i className="ri-equalizer-2-line" aria-hidden="true" />
          </span>
          <div>
            <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-green-deep">{t('admin.list.filters')}</p>
            <p className="hidden text-xs text-ink-variant/70 sm:block">{t('admin.list.filtersHint')}</p>
          </div>
        </div>
        <div className="flex flex-wrap items-end gap-4 p-4 sm:px-5 [&>div]:min-w-[210px] [&>div]:flex-1 lg:[&>div]:max-w-[290px]">{toolbar}</div>
      </div>

      <div className="grid grid-cols-2 gap-3 xl:grid-cols-4">
        <AdminStatCard value={subscriptions.length} label={t('admin.newsletter.statsShowing')} icon="ri-contacts-book-2-line" />
        <AdminStatCard value={activeCount} label={t('admin.newsletter.statsActive')} icon="ri-checkbox-circle-line" tone="green" />
        <AdminStatCard value={inactiveCount} label={t('admin.newsletter.statsInactive')} icon="ri-pause-circle-line" tone="neutral" />
        <AdminStatCard value={frenchCount} label={t('admin.newsletter.statsFrench')} icon="ri-translate-2" tone="gold" />
      </div>

      <div className="grid grid-cols-1 gap-4 xl:grid-cols-[minmax(0,1fr)_360px]">
        <div className="min-w-0">
          {error ? (
            <EmptyState tone="error" title={error} />
          ) : subscriptions.length === 0 ? (
            <EmptyState title={t('admin.newsletter.empty')} />
          ) : (
            <>
            <DataTable
              columns={[
                { key: 'name', label: t('admin.newsletter.colName') },
                { key: 'email', label: t('admin.newsletter.colEmail') },
                { key: 'language', label: t('admin.newsletter.colLanguage') },
                { key: 'source', label: t('admin.newsletter.colSource') },
                { key: 'date', label: t('admin.newsletter.colDate') },
                { key: 'status', label: t('admin.newsletter.colStatus') },
                { key: 'actions', label: t('admin.newsletter.colActions'), align: 'right' },
              ]}
            >
              {subscriptions.map((item) => (
                <tr key={item.id} className="transition-colors hover:bg-surface-container">
                  <Td className="font-medium text-ink">{item.fullName}</Td>
                  <Td>{item.email}</Td>
                  <Td className="uppercase">{item.preferredLanguage}</Td>
                  <Td>{item.source}</Td>
                  <Td>{formatDate(item.createdAt)}</Td>
                  <Td>
                    <StatusChip
                      status={item.isActive ? 'published' : 'draft'}
                      label={item.isActive ? t('admin.newsletter.statusActive') : t('admin.newsletter.statusInactive')}
                    />
                  </Td>
                  <Td align="right">
                    {item.isActive ? (
                      <button
                        type="button"
                        disabled={actionId === item.id}
                        onClick={() => handleDeactivate(item.id)}
                        className="min-h-[44px] text-label-md uppercase text-error transition-colors hover:text-error-deep disabled:opacity-50"
                      >
                        {t('admin.newsletter.deactivate')}
                      </button>
                    ) : (
                      <button
                        type="button"
                        disabled={actionId === item.id}
                        onClick={() => handleReactivate(item.id)}
                        className="min-h-[44px] text-label-md uppercase text-green transition-colors hover:text-green-deep disabled:opacity-50"
                      >
                        {t('admin.newsletter.reactivate')}
                      </button>
                    )}
                  </Td>
                </tr>
              ))}
            </DataTable>
            {totalPages > 1 && (
              <nav className="admin-panel mt-3 flex items-center justify-between gap-3 px-4 py-3" aria-label={t('admin.list.pagination')}>
                <p className="text-xs text-ink-variant">{t('admin.list.totalItems', { count: totalItems })}</p>
                <div className="flex items-center gap-2">
                  <button type="button" className="h-10 rounded-lg border border-line px-3 text-xs font-bold uppercase text-green disabled:opacity-40" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>{t('admin.list.previous')}</button>
                  <span className="text-xs font-semibold text-ink">{page} / {totalPages}</span>
                  <button type="button" className="h-10 rounded-lg border border-line px-3 text-xs font-bold uppercase text-green disabled:opacity-40" disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)}>{t('admin.list.next')}</button>
                </div>
              </nav>
            )}
            </>
          )}
        </div>

        <div className="flex min-w-0 flex-col gap-4">
          <form className="admin-panel relative overflow-hidden p-6" onSubmit={handleCreateCampaign}>
            <span className="mb-5 flex h-11 w-11 items-center justify-center rounded-xl bg-gold/15 text-gold-ink">
              <i className="ri-quill-pen-line text-lg" aria-hidden="true" />
            </span>
            <h2 className="font-display text-headline-sm text-green-deep">{t('admin.newsletter.composeTitle')}</h2>
            <div className="mt-4 space-y-3">
              <div className="grid gap-3 sm:grid-cols-2">
                <Field label={i18n.language.startsWith('fr') ? 'Audience' : 'Audience'} htmlFor="campaign-audience">
                  <select id="campaign-audience" className={inputClasses} value={campaignForm.audience} onChange={(e) => setCampaignForm({ ...campaignForm, audience: e.target.value as CreateNewsletterCampaignRequest['audience'] })}>
                    <option value="Newsletter">Infolettre</option><option value="Members">Membres</option><option value="All">Tous (dédupliqué)</option>
                  </select>
                </Field>
                <Field label={i18n.language.startsWith('fr') ? 'Type de communication' : 'Communication type'} htmlFor="campaign-category">
                  <select id="campaign-category" className={inputClasses} value={campaignForm.preferenceCategory} onChange={(e) => setCampaignForm({ ...campaignForm, preferenceCategory: e.target.value as CreateNewsletterCampaignRequest['preferenceCategory'] })}>
                    <option value="newsletter">Infolettre</option><option value="events">Événements</option><option value="opportunities">Occasions</option><option value="mentorship">Mentorat</option><option value="service">Services</option>
                  </select>
                </Field>
              </div>
              {campaignForm.audience !== 'Newsletter' && <div className="rounded-xl border border-line bg-canvas/45 p-3"><p className="mb-3 text-[9px] font-bold uppercase tracking-[.14em] text-green">Segmentation facultative</p><div className="grid gap-3 sm:grid-cols-2"><input aria-label="Province" placeholder="Province" className={inputClasses} value={campaignForm.targetProvince ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, targetProvince: e.target.value })} /><input aria-label="Zone" placeholder="Zone HCBE" className={inputClasses} value={campaignForm.targetZone ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, targetZone: e.target.value })} /><select aria-label="Langue" className={inputClasses} value={campaignForm.targetLanguage ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, targetLanguage: e.target.value })}><option value="">FR + EN</option><option value="fr">Français</option><option value="en">English</option></select><input aria-label="Intérêt" placeholder="Intérêt contient…" className={inputClasses} value={campaignForm.targetInterest ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, targetInterest: e.target.value })} /></div></div>}
              <Field label={t('admin.newsletter.subjectFr')} htmlFor="campaign-subject" required>
                <input id="campaign-subject" className={inputClasses} required value={campaignForm.subject} onChange={(e) => setCampaignForm({ ...campaignForm, subject: e.target.value })} />
              </Field>
              <Field label={t('admin.newsletter.bodyFr')} htmlFor="campaign-body" required>
                <textarea id="campaign-body" className={`${inputClasses} min-h-28 resize-y`} required value={campaignForm.body} onChange={(e) => setCampaignForm({ ...campaignForm, body: e.target.value })} />
              </Field>
              <Field label={t('admin.newsletter.subjectEn')} htmlFor="campaign-subject-en">
                <input id="campaign-subject-en" className={inputClasses} value={campaignForm.subjectEn} onChange={(e) => setCampaignForm({ ...campaignForm, subjectEn: e.target.value })} />
              </Field>
              <Field label={t('admin.newsletter.bodyEn')} htmlFor="campaign-body-en">
                <textarea id="campaign-body-en" className={`${inputClasses} min-h-24 resize-y`} value={campaignForm.bodyEn} onChange={(e) => setCampaignForm({ ...campaignForm, bodyEn: e.target.value })} />
              </Field>
              <Field label={i18n.language.startsWith('fr') ? 'Programmer l’envoi (facultatif)' : 'Schedule send (optional)'} htmlFor="campaign-schedule">
                <input id="campaign-schedule" type="datetime-local" className={inputClasses} value={campaignForm.scheduledAtUtc ? new Date(campaignForm.scheduledAtUtc).toISOString().slice(0, 16) : ''} onChange={(e) => setCampaignForm({ ...campaignForm, scheduledAtUtc: e.target.value ? new Date(e.target.value).toISOString() : undefined })} />
              </Field>
              <Button type="submit" variant="primary" className="w-full" disabled={campaignBusy}>
                {t('admin.newsletter.saveDraft')}
              </Button>
            </div>
          </form>

          <div className="admin-panel p-5">
            <h2 className="font-display text-headline-sm text-green-deep">{t('admin.newsletter.campaigns')}</h2>
            <div className="mt-4 space-y-3">
              {campaigns.length === 0 && <p className="text-sm text-ink-variant">{t('admin.newsletter.noCampaigns')}</p>}
              {campaigns.slice(0, 6).map((campaign) => (
                <div key={campaign.id} className="rounded-xl border border-line/60 p-3">
                  <div className="flex items-start justify-between gap-3">
                    <p className="font-medium text-ink">{campaign.subject}</p>
                    <span className="text-[10px] font-bold uppercase tracking-wide text-ink-variant">{campaign.status}</span>
                  </div>
                  <p className="mt-1 text-xs text-ink-variant">{campaign.audience} · {campaign.preferenceCategory} · {campaign.sentCount}/{campaign.recipientCount} · {campaign.failedCount} {t('admin.newsletter.failed')}</p>
                  {campaign.status === 'Draft' && (
                    <button type="button" className="mt-3 text-label-md uppercase text-green" disabled={campaignBusy} onClick={() => handleSendCampaign(campaign.id)}>
                      {t('admin.newsletter.sendNow')}
                    </button>
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default NewsletterAdminPage;
