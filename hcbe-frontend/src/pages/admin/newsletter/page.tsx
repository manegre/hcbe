import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { newsletterApi } from '../../../lib/api/newsletter';
import { associationsApi } from '../../../lib/api/associations';
import type { Association, CampaignAudiencePreviewDto, CampaignDeliveryDto, CommunicationConsentEventDto, CreateNewsletterCampaignRequest, NewsletterCampaignDto, NewsletterSubscriptionDto } from '../../../lib/api/types';
import { Button, DataTable, EmptyState, Field, RichTextEditor, StatusChip, Td, inputClasses } from '../../../components/ui';
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
  const [consentHistory, setConsentHistory] = useState<CommunicationConsentEventDto[]>([]);
  const emptyCampaign: CreateNewsletterCampaignRequest = { subject: '', subjectEn: '', body: '', bodyEn: '', audience: 'Newsletter', channels: 'Email', preferenceCategory: 'newsletter' };
  const [campaignForm, setCampaignForm] = useState<CreateNewsletterCampaignRequest>(emptyCampaign);
  const [audiencePreview, setAudiencePreview] = useState<CampaignAudiencePreviewDto | null>(null);
  const [previewBusy, setPreviewBusy] = useState(false);
  const [associations, setAssociations] = useState<Association[]>([]);
  const [deliveryCampaignId, setDeliveryCampaignId] = useState<string | null>(null);
  const [deliveries, setDeliveries] = useState<CampaignDeliveryDto[]>([]);
  const [testEmail, setTestEmail] = useState('');
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
    newsletterApi.getConsentHistory(30).then((response) => { if (response.success && response.data) setConsentHistory(response.data); }).catch(() => undefined);
    associationsApi.getAssociationsForAdmin().then((response) => { if (response.success && response.data) setAssociations(response.data); }).catch(() => undefined);
  }, []);

  const selectedChannels = new Set((campaignForm.channels || 'Email').split(',').filter(Boolean));
  const toggleChannel = (channel: 'Email' | 'InApp' | 'Push') => {
    const next = new Set(selectedChannels);
    if (next.has(channel)) next.delete(channel); else next.add(channel);
    if (next.size === 0) return;
    setCampaignForm({ ...campaignForm, channels: Array.from(next).join(','), audience: channel !== 'Email' && campaignForm.audience === 'Newsletter' ? 'Members' : campaignForm.audience });
    setAudiencePreview(null);
  };

  const handlePreviewAudience = async () => {
    setPreviewBusy(true); setAudiencePreview(null);
    try {
      const response = await newsletterApi.previewCampaign(campaignForm);
      if (response.success && response.data) setAudiencePreview(response.data);
      else setError(response.message || t('admin.newsletter.campaignError'));
    } finally { setPreviewBusy(false); }
  };

  const handleDeliveries = async (id: string) => {
    if (deliveryCampaignId === id) { setDeliveryCampaignId(null); setDeliveries([]); return; }
    const response = await newsletterApi.getCampaignDeliveries(id);
    if (response.success && response.data) { setDeliveryCampaignId(id); setDeliveries(response.data); }
  };

  const handleTestCampaign = async (id: string) => {
    if (!testEmail.trim()) return;
    setCampaignBusy(true);
    try {
      const response = await newsletterApi.sendCampaignTest(id, testEmail.trim());
      if (!response.success) setError(response.message || t('admin.newsletter.campaignError'));
      else await loadCampaigns();
    } finally { setCampaignBusy(false); }
  };

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
  const sentCampaigns = campaigns.filter((item) => item.sentCount > 0);
  const deliveredTotal = sentCampaigns.reduce((sum, item) => sum + item.sentCount, 0);
  const openedTotal = sentCampaigns.reduce((sum, item) => sum + item.openedCount, 0);
  const averageOpenRate = deliveredTotal ? Math.round(openedTotal * 1000 / deliveredTotal) / 10 : 0;
  const deliveryAlerts = campaigns.filter((item) => item.failedCount > 0 || item.pushFailedCount > 0 || item.status === 'Failed' || item.status === 'PartiallySent');

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

      <div className={`admin-panel flex flex-col gap-4 border-l-4 p-5 sm:flex-row sm:items-center sm:justify-between ${deliveryAlerts.length ? 'border-l-error bg-error/[.035]' : 'border-l-green bg-green/[.025]'}`} role="status" aria-live="polite">
        <div className="flex min-w-0 items-start gap-3">
          <span className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ${deliveryAlerts.length ? 'bg-error/10 text-error' : 'bg-green/10 text-green'}`}>
            <i className={deliveryAlerts.length ? 'ri-alarm-warning-line' : 'ri-shield-check-line'} aria-hidden="true" />
          </span>
          <div>
            <p className="text-[9px] font-bold uppercase tracking-[.14em] text-ink-variant">{t('admin.newsletter.deliveryMonitoring')}</p>
            <h2 className="mt-1 font-display text-title-lg text-green-deep">{deliveryAlerts.length ? t('admin.newsletter.deliveryAlertTitle', { count: deliveryAlerts.length }) : t('admin.newsletter.deliveryHealthy')}</h2>
            <p className="mt-1 text-xs leading-relaxed text-ink-variant">{deliveryAlerts.length ? t('admin.newsletter.deliveryAlertHint') : t('admin.newsletter.deliveryHealthyHint')}</p>
          </div>
        </div>
        {deliveryAlerts.length > 0 && <div className="flex flex-wrap gap-2 sm:max-w-md sm:justify-end">{deliveryAlerts.slice(0, 3).map((campaign) => <button key={campaign.id} type="button" onClick={() => handleDeliveries(campaign.id)} className="min-h-10 rounded-full border border-error/20 bg-surface px-3 text-[9px] font-bold uppercase tracking-wide text-error hover:border-error/50">{campaign.subject} · {campaign.failedCount + campaign.pushFailedCount} {t('admin.newsletter.failed')}</button>)}</div>}
      </div>

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

      <div className="grid grid-cols-2 gap-3 xl:grid-cols-4">
        <AdminStatCard value={campaigns.length} label={t('admin.newsletter.campaignsCount')} icon="ri-megaphone-line" />
        <AdminStatCard value={deliveredTotal} label={t('admin.newsletter.delivered')} icon="ri-mail-check-line" tone="green" />
        <AdminStatCard value={`${averageOpenRate}%`} label={t('admin.newsletter.openRate')} icon="ri-eye-line" tone="gold" />
        <AdminStatCard value={campaigns.reduce((sum, item) => sum + item.unsubscribedCount, 0)} label={t('admin.newsletter.attributedOptOuts')} icon="ri-user-unfollow-line" tone="neutral" />
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
                <Field label={t('admin.newsletter.audienceLabel')} htmlFor="campaign-audience">
                  <select id="campaign-audience" className={inputClasses} value={campaignForm.audience} onChange={(e) => setCampaignForm({ ...campaignForm, audience: e.target.value as CreateNewsletterCampaignRequest['audience'] })}>
                    <option value="Newsletter">{t('admin.newsletter.audienceNewsletter')}</option><option value="Members">{t('admin.newsletter.audienceMembers')}</option><option value="All">{t('admin.newsletter.audienceAll')}</option>
                  </select>
                </Field>
                <Field label={t('admin.newsletter.categoryLabel')} htmlFor="campaign-category">
                  <select id="campaign-category" className={inputClasses} value={campaignForm.preferenceCategory} onChange={(e) => setCampaignForm({ ...campaignForm, preferenceCategory: e.target.value as CreateNewsletterCampaignRequest['preferenceCategory'] })}>
                    {['newsletter', 'events', 'opportunities', 'mentorship', 'service'].map((category) => <option key={category} value={category}>{t(`admin.newsletter.category.${category}`)}</option>)}
                  </select>
                </Field>
              </div>
              <fieldset className="rounded-2xl border border-line bg-canvas/45 p-4">
                <legend className="px-2 text-[9px] font-bold uppercase tracking-[.14em] text-green">{t('admin.newsletter.channels')}</legend>
                <div className="grid gap-2 sm:grid-cols-3">
                  {([['Email', 'ri-mail-send-line'], ['InApp', 'ri-notification-3-line'], ['Push', 'ri-smartphone-line']] as const).map(([channel, icon]) => (
                    <button key={channel} type="button" aria-pressed={selectedChannels.has(channel)} onClick={() => toggleChannel(channel)} className={`flex min-h-14 items-center gap-3 rounded-xl border px-3 text-left transition ${selectedChannels.has(channel) ? 'border-green bg-green text-white shadow-sm' : 'border-line bg-surface text-ink hover:border-green/35'}`}>
                      <i className={`${icon} text-lg`} aria-hidden="true" /><span className="text-xs font-bold">{t(`admin.newsletter.channel.${channel}`)}</span>
                    </button>
                  ))}
                </div>
              </fieldset>
              {campaignForm.audience !== 'Newsletter' && <div className="rounded-2xl border border-line bg-canvas/45 p-4"><p className="mb-3 text-[9px] font-bold uppercase tracking-[.14em] text-green">{t('admin.newsletter.segmentation')}</p><div className="grid gap-3 sm:grid-cols-2"><input aria-label={t('admin.members.province')} placeholder={t('admin.members.province')} className={inputClasses} value={campaignForm.targetProvince ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, targetProvince: e.target.value })} /><input aria-label={t('admin.common.zone')} placeholder={t('admin.newsletter.zonePlaceholder')} className={inputClasses} value={campaignForm.targetZone ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, targetZone: e.target.value })} /><select aria-label={t('admin.common.language')} className={inputClasses} value={campaignForm.targetLanguage ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, targetLanguage: e.target.value })}><option value="">{t('admin.newsletter.bothLanguages')}</option><option value="fr">{t('admin.newsletter.filterLanguageFr')}</option><option value="en">{t('admin.newsletter.filterLanguageEn')}</option></select><input aria-label={t('admin.newsletter.interest')} placeholder={t('admin.newsletter.interestPlaceholder')} className={inputClasses} value={campaignForm.targetInterest ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, targetInterest: e.target.value })} /><select aria-label={t('admin.newsletter.membershipStatus')} className={inputClasses} value={campaignForm.targetMembershipStatus ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, targetMembershipStatus: e.target.value || undefined })}><option value="">{t('admin.newsletter.allMembershipStatuses')}</option>{['Active', 'GracePeriod', 'Inactive', 'Expired'].map((status) => <option key={status} value={status}>{t(`admin.newsletter.membership.${status}`)}</option>)}</select><select aria-label={t('admin.newsletter.association')} className={inputClasses} value={campaignForm.targetAssociationId ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, targetAssociationId: e.target.value || undefined })}><option value="">{t('admin.newsletter.allAssociations')}</option>{associations.map((association) => <option key={association.id} value={association.id}>{association.name}</option>)}</select></div></div>}
              <Field label={t('admin.newsletter.subjectFr')} htmlFor="campaign-subject" required>
                <input id="campaign-subject" className={inputClasses} required value={campaignForm.subject} onChange={(e) => setCampaignForm({ ...campaignForm, subject: e.target.value })} />
              </Field>
              <Field label={t('admin.newsletter.bodyFr')} htmlFor="campaign-body" required>
                <RichTextEditor id="campaign-body" value={campaignForm.body} onChange={(body) => setCampaignForm({ ...campaignForm, body })} minHeight={220} required />
              </Field>
              <Field label={t('admin.newsletter.subjectEn')} htmlFor="campaign-subject-en">
                <input id="campaign-subject-en" className={inputClasses} value={campaignForm.subjectEn} onChange={(e) => setCampaignForm({ ...campaignForm, subjectEn: e.target.value })} />
              </Field>
              <Field label={t('admin.newsletter.bodyEn')} htmlFor="campaign-body-en">
                <RichTextEditor id="campaign-body-en" value={campaignForm.bodyEn ?? ''} onChange={(bodyEn) => setCampaignForm({ ...campaignForm, bodyEn })} minHeight={200} />
              </Field>
              <Field label={t('admin.newsletter.schedule')} htmlFor="campaign-schedule">
                <input id="campaign-schedule" type="datetime-local" className={inputClasses} value={campaignForm.scheduledAtUtc ? new Date(campaignForm.scheduledAtUtc).toISOString().slice(0, 16) : ''} onChange={(e) => setCampaignForm({ ...campaignForm, scheduledAtUtc: e.target.value ? new Date(e.target.value).toISOString() : undefined })} />
              </Field>
              <div className="rounded-2xl border border-green/15 bg-green/[.035] p-4">
                <div className="flex flex-wrap items-center justify-between gap-3"><div><p className="text-[9px] font-bold uppercase tracking-[.14em] text-green">{t('admin.newsletter.audiencePreview')}</p><p className="mt-1 text-xs text-ink-variant">{t('admin.newsletter.audiencePreviewHint')}</p></div><Button type="button" variant="tertiary" disabled={previewBusy} onClick={handlePreviewAudience}>{previewBusy ? <i className="ri-loader-4-line animate-spin" /> : <i className="ri-radar-line" />}{t('admin.newsletter.calculateAudience')}</Button></div>
                {audiencePreview && <div className="mt-4 grid grid-cols-2 gap-2 sm:grid-cols-4">{([['uniqueRecipients', 'ri-group-line'], ['emailRecipients', 'ri-mail-line'], ['inAppRecipients', 'ri-notification-line'], ['pushReadyRecipients', 'ri-smartphone-line']] as const).map(([key, icon]) => <div key={key} className="rounded-xl bg-surface p-3 text-center"><i className={`${icon} text-green`} /><strong className="mt-1 block font-display text-xl text-green-deep">{audiencePreview[key]}</strong><span className="text-[8px] font-bold uppercase tracking-wide text-ink-variant">{t(`admin.newsletter.preview.${key}`)}</span></div>)}</div>}
              </div>
              <Button type="submit" variant="primary" className="w-full" disabled={campaignBusy}><i className="ri-draft-line" />{t('admin.newsletter.saveDraft')}</Button>
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
                    <span className="text-[10px] font-bold uppercase tracking-wide text-ink-variant">{t(`admin.newsletter.status.${campaign.status}`, { defaultValue: campaign.status })}</span>
                  </div>
                  <p className="mt-1 text-xs text-ink-variant">{t(`admin.newsletter.audience${campaign.audience}`)} · {t(`admin.newsletter.category.${campaign.preferenceCategory}`, { defaultValue: campaign.preferenceCategory })}</p>
                  <div className="mt-2 flex flex-wrap gap-1.5">{campaign.channels.split(',').map((channel) => <span key={channel} className="rounded-full border border-green/15 bg-green/5 px-2 py-1 text-[8px] font-bold uppercase tracking-wide text-green">{t(`admin.newsletter.channel.${channel}`)}</span>)}</div>
                  <div className="mt-3 grid grid-cols-2 divide-x divide-y divide-line rounded-lg bg-canvas/60 py-2 text-center sm:grid-cols-4 sm:divide-y-0"><div className="py-1"><strong className="block text-sm text-green-deep">{campaign.sentCount}</strong><span className="text-[8px] uppercase text-ink-variant">{t('admin.newsletter.channel.Email')}</span></div><div className="py-1"><strong className="block text-sm text-green-deep">{campaign.inAppSentCount}</strong><span className="text-[8px] uppercase text-ink-variant">{t('admin.newsletter.channel.InApp')}</span></div><div className="py-1"><strong className="block text-sm text-green-deep">{campaign.pushSentCount}</strong><span className="text-[8px] uppercase text-ink-variant">{t('admin.newsletter.channel.Push')}</span></div><div className="py-1"><strong className="block text-sm text-green-deep">{campaign.openRate}%</strong><span className="text-[8px] uppercase text-ink-variant">{t('admin.newsletter.rate')}</span></div></div>
                  {(campaign.failedCount > 0 || campaign.pushFailedCount > 0) && <p className="mt-2 rounded-lg bg-error/8 px-3 py-2 text-[10px] font-semibold text-error">{t('admin.newsletter.deliveryFailures', { email: campaign.failedCount, push: campaign.pushFailedCount })}</p>}
                  <div className="mt-3 flex flex-wrap items-center gap-3">
                    {campaign.status === 'Draft' && <button type="button" className="text-label-md uppercase text-green" disabled={campaignBusy} onClick={() => handleSendCampaign(campaign.id)}>{t('admin.newsletter.sendNow')}</button>}
                    <button type="button" className="text-label-md uppercase text-green" onClick={() => handleDeliveries(campaign.id)}>{deliveryCampaignId === campaign.id ? t('admin.newsletter.hideDeliveries') : t('admin.newsletter.showDeliveries')}</button>
                  </div>
                  {campaign.status === 'Draft' && <div className="mt-3 flex gap-2"><input type="email" aria-label={t('admin.newsletter.testEmail')} placeholder={t('admin.newsletter.testEmail')} className={`${inputClasses} min-w-0 flex-1`} value={testEmail} onChange={(event) => setTestEmail(event.target.value)} /><Button type="button" variant="tertiary" disabled={!testEmail.trim() || campaignBusy} onClick={() => handleTestCampaign(campaign.id)}>{t('admin.newsletter.sendTest')}</Button></div>}
                  {deliveryCampaignId === campaign.id && <div className="mt-3 max-h-64 space-y-2 overflow-y-auto rounded-xl border border-line bg-canvas/40 p-2">{deliveries.length === 0 ? <p className="p-2 text-xs text-ink-variant">{t('admin.newsletter.noDeliveries')}</p> : deliveries.map((delivery) => <div key={delivery.id} className="rounded-lg bg-surface p-2.5"><p className="truncate text-xs font-semibold text-ink">{delivery.recipient}</p><div className="mt-1 flex flex-wrap gap-2 text-[8px] font-bold uppercase text-ink-variant"><span>Email: {delivery.emailStatus}</span><span>In-app: {delivery.inAppStatus}</span><span>Push: {delivery.pushStatus}</span><span>{delivery.openCount} {t('admin.newsletter.opens')}</span></div>{delivery.failureReason && <p className="mt-1 text-[10px] text-error">{delivery.failureReason}</p>}</div>)}</div>}
                </div>
              ))}
            </div>
          </div>
          <div className="admin-panel p-5">
            <div className="flex items-center justify-between gap-3"><div><p className="text-[9px] font-bold uppercase tracking-[.14em] text-red-link">{t('admin.newsletter.law25')}</p><h2 className="mt-1 font-display text-headline-sm text-green-deep">{t('admin.newsletter.consentHistory')}</h2></div><i className="ri-shield-check-line text-2xl text-green/50" /></div>
            <div className="mt-4 max-h-80 space-y-2 overflow-y-auto pr-1">{consentHistory.length === 0 ? <p className="text-sm text-ink-variant">{t('admin.newsletter.consentEmpty')}</p> : consentHistory.map((item) => <div key={item.id} className="rounded-xl border border-line/60 p-3"><div className="flex items-center justify-between gap-3"><strong className="truncate text-xs text-ink">{item.email}</strong><span className={`rounded-full px-2 py-1 text-[8px] font-bold uppercase ${item.action === 'OptIn' ? 'bg-green/10 text-green' : 'bg-error/10 text-error'}`}>{t(`admin.newsletter.action.${item.action}`, { defaultValue: item.action })}</span></div><p className="mt-1 text-[10px] text-ink-variant">{t(`admin.newsletter.category.${item.category}`, { defaultValue: item.category })} · {t(`admin.newsletter.source.${item.source}`, { defaultValue: item.source })} · {formatDate(item.occurredAtUtc)}</p></div>)}</div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default NewsletterAdminPage;
