import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { grantsApi } from '../../../lib/api/grants';
import type { GrantProgram } from '../../../lib/api/types';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { StatusChip, Td } from '../../../components/ui';

const GrantsAdminPage: React.FC = () => {
  const { t } = useTranslation();
  const [grants, setGrants] = useState<GrantProgram[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadGrants = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await grantsApi.getGrantsForAdmin();
      if (response.success && response.data) {
        setGrants(response.data);
      } else {
        setError(t('admin.grants.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading grants:', err);
      setError(t('admin.grants.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadGrants();
  }, []);

  const handleToggleStatus = async (id: string) => {
    try {
      const response = await grantsApi.toggleGrantStatus(id);
      if (response.success) {
        loadGrants();
      }
    } catch (err) {
      console.error('Error toggling grant status:', err);
    }
  };

  const handleDelete = async (id: string, title: string) => {
    if (!window.confirm(t('admin.common.confirmDelete', { name: title }))) return;

    try {
      const response = await grantsApi.deleteGrant(id);
      if (response.success) {
        loadGrants();
      }
    } catch (err) {
      console.error('Error deleting grant:', err);
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
      title={t('admin.grants.title')}
      count={error ? undefined : grants.length}
      createLabel={t('admin.grants.create')}
      createPath="/admin/grants/create"
      columns={[
        { key: 'program', label: t('admin.grants.colProgram') },
        { key: 'amount', label: t('admin.grants.colAmount') },
        { key: 'duration', label: t('admin.grants.colDuration') },
        { key: 'status', label: t('admin.common.status') },
        { key: 'order', label: t('admin.grants.colOrder') },
        { key: 'actions', label: t('admin.common.actions'), align: 'right' },
      ]}
      isEmpty={grants.length === 0}
      emptyTitle={t('admin.grants.emptyTitle')}
      error={error ?? undefined}
      onRetry={loadGrants}
    >
      {grants.map((grant) => (
        <tr key={grant.id} className="transition-colors hover:bg-surface-container">
          <Td className="text-ink">
            <div className="flex items-center gap-3">
              <i className={`${grant.icon} text-lg text-ink-variant`} aria-hidden="true" />
              <div>
                <div className="font-medium">{grant.title}</div>
                <div className="max-w-xs truncate text-body-md text-ink-variant">{grant.description}</div>
              </div>
            </div>
          </Td>
          <Td>{grant.amount}</Td>
          <Td>{grant.duration}</Td>
          <Td>
            <StatusChip
              status={grant.isActive ? 'published' : 'draft'}
              label={grant.isActive ? t('admin.common.active') : t('admin.common.inactive')}
            />
          </Td>
          <Td>{grant.displayOrder}</Td>
          <Td align="right">
            <div className="inline-flex items-center justify-end gap-1">
              <Link
                to={`/admin/grants/${grant.id}`}
                aria-label={t('admin.common.view')}
                title={t('admin.common.view')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-eye-line text-lg" aria-hidden="true" />
              </Link>
              <Link
                to={`/admin/grants/${grant.id}/edit`}
                aria-label={t('admin.common.edit')}
                title={t('admin.common.edit')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-edit-line text-lg" aria-hidden="true" />
              </Link>
              <button
                type="button"
                onClick={() => handleToggleStatus(grant.id)}
                aria-label={grant.isActive ? t('admin.grants.deactivate') : t('admin.grants.activate')}
                title={grant.isActive ? t('admin.grants.deactivate') : t('admin.grants.activate')}
                className={`inline-flex min-h-[44px] min-w-[44px] items-center justify-center transition-colors ${
                  grant.isActive ? 'text-gold-ink hover:text-green' : 'text-green hover:text-green-deep'
                }`}
              >
                <i className={grant.isActive ? 'ri-pause-circle-line text-lg' : 'ri-play-circle-line text-lg'} aria-hidden="true" />
              </button>
              <button
                type="button"
                onClick={() => handleDelete(grant.id, grant.title)}
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

export default GrantsAdminPage;
