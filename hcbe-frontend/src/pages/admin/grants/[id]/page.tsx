import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { grantsApi } from '../../../../lib/api/grants';
import type { GrantProgram } from '../../../../lib/api/types';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState, RichTextContent } from '../../../../components/ui';

const GrantViewPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [grant, setGrant] = useState<GrantProgram | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadGrant = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const response = await grantsApi.getGrantForAdmin(id);
        if (response.success && response.data) {
          setGrant(response.data);
        } else {
          setError(t('admin.grants.errorLoad'));
        }
      } catch (err) {
        console.error('Error loading grant:', err);
        setError(t('admin.grants.errorLoad'));
      } finally {
        setLoading(false);
      }
    };

    loadGrant();
  }, [id, t]);

  const handleDelete = async () => {
    if (!id || !grant) return;
    if (!window.confirm(t('admin.common.confirmDelete', { name: grant.title }))) return;

    try {
      const response = await grantsApi.deleteGrant(id);
      if (response.success) {
        navigate('/admin/grants');
      }
    } catch (err) {
      console.error('Error deleting grant:', err);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error || !grant) {
    return (
      <EmptyState
        tone="error"
        title={error || t('admin.grants.errorLoad')}
        action={
          <Button to="/admin/grants" variant="secondary">
            {t('admin.common.backToList')}
          </Button>
        }
      />
    );
  }

  return (
    <AdminDetailLayout
      title={grant.title}
      subtitle={`${grant.isActive ? t('admin.common.active') : t('admin.common.inactive')} · ${t('admin.grants.colOrder')}: ${grant.displayOrder}`}
      backPath="/admin/grants"
      secondaryActions={<i className={`${grant.icon} text-2xl text-green`} aria-hidden="true"></i>}
      actions={
        <>
          <Button to={`/admin/grants/${grant.id}/edit`} variant="secondary">
            {t('admin.common.edit')}
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            {t('admin.common.delete')}
          </Button>
        </>
      }
      main={
        <>
          <RichTextContent value={grant.description} className="text-body-md text-ink-variant" />
          <DetailList>
            <DetailRow label={t('admin.grants.colAmount')} value={grant.amount} />
            <DetailRow label={t('admin.grants.colDuration')} value={grant.duration} />
            {grant.applicationUrl && (
              <DetailRow
                label={t('admin.grants.applicationUrl')}
                value={
                  <a
                    href={grant.applicationUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-red-link hover:text-green"
                  >
                    {grant.applicationUrl}
                  </a>
                }
              />
            )}
          </DetailList>

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.grants.criteria')}</h2>
            <ul className="mt-4 space-y-2">
              {grant.eligibilityCriteria.map((criterion) => (
                <li key={criterion} className="flex items-start gap-2 text-body-md text-ink-variant">
                  <i className="ri-checkbox-circle-line mt-0.5 text-green" aria-hidden="true"></i>
                  {criterion}
                </li>
              ))}
            </ul>
          </div>
        </>
      }
    />
  );
};

export default GrantViewPage;
