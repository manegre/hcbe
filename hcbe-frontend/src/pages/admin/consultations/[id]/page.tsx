import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { consultationsApi } from '../../../../lib/api/consultations';
import type { Consultation, ConsultationAuditEvent } from '../../../../lib/api/types';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState } from '../../../../components/ui';

const ConsultationViewPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [item, setItem] = useState<Consultation | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [audit, setAudit] = useState<ConsultationAuditEvent[]>([]);
  const [publishing, setPublishing] = useState(false);

  useEffect(() => {
    const loadItem = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const [response, auditResponse] = await Promise.all([
          consultationsApi.getConsultationForAdmin(id),
          consultationsApi.getAudit(id),
        ]);
        if (response.success && response.data) {
          setItem(response.data);
          if (auditResponse.success && auditResponse.data) setAudit(auditResponse.data);
        } else {
          setError(response.message || t('admin.consultations.errorLoad'));
        }
      } catch (err) {
        console.error('Error loading consultation:', err);
        setError(err instanceof Error ? err.message : t('admin.consultations.errorLoad'));
      } finally {
        setLoading(false);
      }
    };

    loadItem();
  }, [id, t]);

  const handleDelete = async () => {
    if (!id || !item) return;
    if (!window.confirm(t('admin.common.confirmDelete', { name: item.title }))) return;

    try {
      const response = await consultationsApi.deleteConsultation(id);
      if (response.success) {
        navigate('/admin/consultations');
      }
    } catch (err) {
      console.error('Error deleting consultation:', err);
    }
  };

  const toggleResults = async () => {
    if (!id || !item) return;
    setPublishing(true);
    try {
      const response = await consultationsApi.publishResults(id, !item.governance?.resultsPublished);
      if (response.success && response.data) setItem(response.data);
      else setError(response.message || t('admin.consultations.errorUpdate'));
    } finally { setPublishing(false); }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error || !item) {
    return (
      <EmptyState
        tone="error"
        title={error || t('admin.consultations.errorLoad')}
        action={
          <Button to="/admin/consultations" variant="secondary">
            {t('admin.common.backToList')}
          </Button>
        }
      />
    );
  }

  return (
    <AdminDetailLayout
      title={item.title}
      subtitle={`${item.isActive ? t('admin.common.active') : t('admin.common.inactive')} · ${
        item.layoutType === 'featured' ? t('admin.consultations.layoutFeatured') : t('admin.consultations.layoutCard')
      } · ${t('admin.consultations.colOrder')}: ${item.displayOrder}`}
      backPath="/admin/consultations"
      secondaryActions={<i className={`${item.icon} text-2xl text-green`} aria-hidden="true"></i>}
      actions={
        <>
          <Button to={`/admin/consultations/${item.id}/edit`} variant="secondary">
            {t('admin.common.edit')}
          </Button>
          {item.governance?.status === 'Closed' && item.options.length > 0 && (
            <Button variant="primary" onClick={toggleResults} disabled={publishing}>
              {item.governance.resultsPublished ? t('admin.consultations.governance.unpublishResults') : t('admin.consultations.governance.publishResults')}
            </Button>
          )}
          <Button variant="destructive" onClick={handleDelete}>
            {t('admin.common.delete')}
          </Button>
        </>
      }
      main={
        <>
          <p className="text-body-md text-ink-variant">{item.description}</p>
          <DetailList>
            <DetailRow label={t('admin.consultations.governance.type')} value={t(`admin.consultations.governance.typeValue.${item.governanceType}`)} />
            <DetailRow label={t('admin.consultations.governance.mode')} value={t(`admin.consultations.governance.modeValue.${item.votingMode}`)} />
            <DetailRow label={t('admin.consultations.governance.eligibility')} value={t(`admin.consultations.governance.eligibilityValue.${item.eligibilityRule}`)} />
            {item.actionUrl && <DetailRow label={t('admin.consultations.actionUrl')} value={item.actionUrl} />}
            {item.actionLabel && <DetailRow label={t('admin.consultations.actionLabel')} value={item.actionLabel} />}
            {item.secondaryActionUrl && (
              <DetailRow label={t('admin.consultations.secondaryActionUrl')} value={item.secondaryActionUrl} />
            )}
            {item.secondaryActionLabel && (
              <DetailRow label={t('admin.consultations.secondaryActionLabel')} value={item.secondaryActionLabel} />
            )}
          </DetailList>
          {item.governance && item.options.length > 0 && (
            <section className="mt-8 rounded-[18px] border border-line p-6">
              <div className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-label-sm uppercase tracking-widest text-red">{t('admin.consultations.governance.results')}</p><h2 className="mt-1 font-display text-headline-md text-green">{item.governance.participantCount} {t('admin.consultations.governance.participants')}</h2></div><span className={`rounded-full px-4 py-2 text-label-sm uppercase ${item.governance.quorumReached ? 'bg-green/10 text-green' : 'bg-gold/20 text-gold-ink'}`}>{item.governance.quorumReached ? t('admin.consultations.governance.quorumReached') : t('admin.consultations.governance.quorumPending')}</span></div>
              <div className="mt-6 space-y-4">{item.governance.results.map(result => <div key={result.optionId}><div className="flex justify-between gap-4 text-body-sm"><span>{result.label}</span><strong>{result.voteCount} · {result.percentage}%</strong></div><div className="mt-2 h-2 rounded-full bg-surface-container"><div className="h-full rounded-full bg-gold" style={{ width: `${result.percentage}%` }} /></div></div>)}</div>
            </section>
          )}
          <section className="mt-8 rounded-[18px] border border-line p-6">
            <h2 className="font-display text-headline-sm text-green">{t('admin.consultations.governance.audit')}</h2>
            <div className="mt-5 divide-y divide-line">{audit.map(event => <div key={event.id} className="grid gap-1 py-4 text-body-sm md:grid-cols-[10rem_1fr_auto]"><strong className="text-green">{event.action}</strong><span className="text-ink-variant">{event.actor || t('admin.consultations.governance.systemOrAnonymous')}</span><time className="text-ink-variant">{new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(event.createdAtUtc))}</time></div>)}</div>
          </section>
        </>
      }
    />
  );
};

export default ConsultationViewPage;
