import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { consultationsApi } from '../../../../lib/api/consultations';
import type { Consultation } from '../../../../lib/api/types';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState } from '../../../../components/ui';

const ConsultationViewPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [item, setItem] = useState<Consultation | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadItem = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const response = await consultationsApi.getConsultationForAdmin(id);
        if (response.success && response.data) {
          setItem(response.data);
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
          <Button variant="destructive" onClick={handleDelete}>
            {t('admin.common.delete')}
          </Button>
        </>
      }
      main={
        <>
          <p className="text-body-md text-ink-variant">{item.description}</p>
          <DetailList>
            {item.actionUrl && <DetailRow label={t('admin.consultations.actionUrl')} value={item.actionUrl} />}
            {item.actionLabel && <DetailRow label={t('admin.consultations.actionLabel')} value={item.actionLabel} />}
            {item.secondaryActionUrl && (
              <DetailRow label={t('admin.consultations.secondaryActionUrl')} value={item.secondaryActionUrl} />
            )}
            {item.secondaryActionLabel && (
              <DetailRow label={t('admin.consultations.secondaryActionLabel')} value={item.secondaryActionLabel} />
            )}
          </DetailList>
        </>
      }
    />
  );
};

export default ConsultationViewPage;
