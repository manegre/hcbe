import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { consultationsApi } from '../../../lib/api/consultations';
import type { Consultation } from '../../../lib/api/types';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { StatusChip, Td } from '../../../components/ui';

const ConsultationsAdminPage: React.FC = () => {
  const { t } = useTranslation();
  const [consultations, setConsultations] = useState<Consultation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadConsultations = async () => {
    try {
      setLoading(true);
      const response = await consultationsApi.getConsultationsForAdmin();
      if (response.success && response.data) {
        setConsultations(response.data);
        setError(null);
      } else {
        setError(response.message || t('admin.consultations.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading consultations:', err);
      setError(err instanceof Error ? err.message : t('admin.consultations.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadConsultations();
  }, []);

  const handleToggleStatus = async (id: string) => {
    try {
      const response = await consultationsApi.toggleConsultationStatus(id);
      if (response.success) {
        loadConsultations();
      }
    } catch (err) {
      console.error('Error toggling consultation status:', err);
    }
  };

  const handleDelete = async (id: string, title: string) => {
    if (!window.confirm(t('admin.common.confirmDelete', { name: title }))) return;

    try {
      const response = await consultationsApi.deleteConsultation(id);
      if (response.success) {
        loadConsultations();
      }
    } catch (err) {
      console.error('Error deleting consultation:', err);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <AdminListPage
      title={t('admin.consultations.title')}
      count={error ? undefined : consultations.length}
      createLabel={t('admin.consultations.create')}
      createPath="/admin/consultations/create"
      columns={[
        { key: 'item', label: t('admin.consultations.colItem') },
        { key: 'layout', label: t('admin.consultations.colLayout') },
        { key: 'status', label: t('admin.common.status') },
        { key: 'order', label: t('admin.consultations.colOrder') },
        { key: 'actions', label: t('admin.common.actions'), align: 'right' },
      ]}
      isEmpty={consultations.length === 0}
      emptyTitle={t('admin.consultations.emptyTitle')}
      error={error ?? undefined}
      onRetry={loadConsultations}
    >
      {consultations.map((item) => (
        <tr key={item.id} className="transition-colors hover:bg-surface-container">
          <Td className="text-ink">
            <div className="flex items-center gap-3">
              <i className={`${item.icon} text-lg text-ink-variant`} aria-hidden="true" />
              <div>
                <div className="font-medium">{item.title}</div>
                <div className="max-w-xs truncate text-body-md text-ink-variant">{item.description}</div>
              </div>
            </div>
          </Td>
          <Td>
            {item.layoutType === 'featured'
              ? t('admin.consultations.layoutFeatured')
              : t('admin.consultations.layoutCard')}
          </Td>
          <Td>
            <StatusChip
              status={item.isActive ? 'published' : 'draft'}
              label={item.isActive ? t('admin.common.active') : t('admin.common.inactive')}
            />
          </Td>
          <Td>{item.displayOrder}</Td>
          <Td align="right">
            <div className="inline-flex items-center justify-end gap-1">
              <Link
                to={`/admin/consultations/${item.id}`}
                aria-label={t('admin.common.view')}
                title={t('admin.common.view')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-eye-line text-lg" aria-hidden="true" />
              </Link>
              <Link
                to={`/admin/consultations/${item.id}/edit`}
                aria-label={t('admin.common.edit')}
                title={t('admin.common.edit')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-edit-line text-lg" aria-hidden="true" />
              </Link>
              <button
                type="button"
                onClick={() => handleToggleStatus(item.id)}
                aria-label={item.isActive ? t('admin.consultations.deactivate') : t('admin.consultations.activate')}
                title={item.isActive ? t('admin.consultations.deactivate') : t('admin.consultations.activate')}
                className={`inline-flex min-h-[44px] min-w-[44px] items-center justify-center transition-colors ${
                  item.isActive ? 'text-gold-ink hover:text-green' : 'text-green hover:text-green-deep'
                }`}
              >
                <i className={item.isActive ? 'ri-pause-circle-line text-lg' : 'ri-play-circle-line text-lg'} aria-hidden="true" />
              </button>
              <button
                type="button"
                onClick={() => handleDelete(item.id, item.title)}
                aria-label={t('admin.common.delete')}
                title={t('admin.common.delete')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center rounded-control text-error transition-colors hover:text-error-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-error"
              >
                <i className="ri-delete-bin-line text-lg" aria-hidden="true" />
              </button>
            </div>
          </Td>
        </tr>
      ))}
    </AdminListPage>
  );
};

export default ConsultationsAdminPage;
