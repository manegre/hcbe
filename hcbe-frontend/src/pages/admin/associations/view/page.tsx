import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { associationsApi } from '../../../../lib/api/associations';
import type { Association } from '../../../../lib/api/types';
import { resolveMediaUrl } from '../../../../lib/api/media-url';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState, RichTextContent, Tag } from '../../../../components/ui';
import { OrganizationWorkspaceAdmin } from './OrganizationWorkspaceAdmin';

export const ViewAssociationPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const [association, setAssociation] = useState<Association | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!id) return;

    const loadAssociation = async () => {
      try {
        setIsLoading(true);
        setError('');
        const response = await associationsApi.getAssociationForAdmin(id);
        if (response.success && response.data) {
          setAssociation(response.data);
        } else {
          setError(t('admin.associations.errorNotFound'));
        }
      } catch (err) {
        console.error('Error loading association:', err);
        setError(t('admin.associations.errorLoad'));
      } finally {
        setIsLoading(false);
      }
    };

    loadAssociation();
  }, [id, t]);

  const handleDelete = async () => {
    if (!association || !window.confirm(t('admin.common.confirmDelete', { name: association.name }))) {
      return;
    }

    try {
      const response = await associationsApi.deleteAssociation(association.id);
      if (response.success) {
        navigate('/admin/associations');
      } else {
        setError(t('admin.associations.errorDelete'));
      }
    } catch (err) {
      console.error('Error deleting association:', err);
      setError(t('admin.associations.errorDelete'));
    }
  };

  const locale = i18n.language.startsWith('fr') ? 'fr-CA' : 'en-CA';
  const formatDateTime = (value: string) =>
    new Date(value).toLocaleString(locale, {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error || !association) {
    return (
      <EmptyState
        tone="error"
        title={error || t('admin.associations.errorNotFound')}
        action={
          <Button to="/admin/associations" variant="secondary">
            {t('admin.common.backToList')}
          </Button>
        }
      />
    );
  }

  return (
    <AdminDetailLayout
      title={association.name}
      subtitle={`${association.city}, ${association.province}`}
      backPath="/admin/associations"
      status={{
        status: association.isActive ? 'published' : 'draft',
        label: association.isActive ? t('admin.common.active') : t('admin.common.inactive'),
      }}
      actions={
        <>
          <Button to={`/admin/associations/${association.id}/edit`} variant="secondary">
            <i className="ri-edit-line" aria-hidden="true" />
            {t('admin.common.edit')}
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            <i className="ri-delete-bin-line" aria-hidden="true" />
            {t('admin.common.delete')}
          </Button>
        </>
      }
      main={
        <>
          {error && <div className="border border-error bg-surface px-4 py-3 text-error">{error}</div>}

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.associations.sectionBasic')}</h2>
            <DetailList>
              {association.foundedYear && (
                <DetailRow label={t('admin.associations.foundedLabel')} value={association.foundedYear} />
              )}
              {association.memberCount && (
                <DetailRow label={t('admin.associations.colMembers')} value={association.memberCount} />
              )}
              {association.president && (
                <DetailRow label={t('admin.associations.president')} value={association.president} />
              )}
            </DetailList>
          </div>

          {association.description && (
            <div>
              <h2 className="font-display text-headline-sm text-green">{t('admin.associations.description')}</h2>
              <RichTextContent value={association.description} className="mt-3 text-body-md text-ink-variant" />
            </div>
          )}

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.associations.sectionDomains')}</h2>
            <div className="mt-3 flex flex-wrap gap-2">
              {association.domains.map((domain) => (
                <Tag key={domain}>{domain}</Tag>
              ))}
            </div>
          </div>

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.associations.sectionContact')}</h2>
            <DetailList>
              <DetailRow
                label={t('admin.associations.locationLabel')}
                value={`${association.city}, ${association.province}`}
              />
              {association.contact && (
                <DetailRow
                  label={t('admin.associations.emailLabel')}
                  value={
                    <a href={`mailto:${association.contact}`} className="text-red-link hover:text-green">
                      {association.contact}
                    </a>
                  }
                />
              )}
              {association.phone && (
                <DetailRow
                  label={t('admin.associations.phone')}
                  value={
                    <a href={`tel:${association.phone}`} className="text-red-link hover:text-green">
                      {association.phone}
                    </a>
                  }
                />
              )}
              {association.website && (
                <DetailRow
                  label={t('admin.associations.website')}
                  value={
                    <a
                      href={association.website}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-1 text-red-link hover:text-green"
                    >
                      {association.website}
                      <i className="ri-external-link-line" aria-hidden="true"></i>
                    </a>
                  }
                />
              )}
            </DetailList>
          </div>

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.associations.sectionSystem')}</h2>
            <DetailList>
              <DetailRow label={t('admin.associations.idLabel')} value={<span className="font-mono">{association.id}</span>} />
              <DetailRow label={t('admin.associations.createdLabel')} value={formatDateTime(association.createdAt)} />
              <DetailRow label={t('admin.associations.updatedLabel')} value={formatDateTime(association.updatedAt)} />
            </DetailList>
          </div>

        </>
      }
      aside={association.imageUrl ? (
        <img
          src={resolveMediaUrl(association.imageUrl)}
          alt={association.name}
          className="h-72 w-full object-cover"
          onError={(e) => {
            e.currentTarget.style.display = 'none';
          }}
        />
      ) : undefined}
      after={<OrganizationWorkspaceAdmin associationId={association.id} />}
    />
  );
};
