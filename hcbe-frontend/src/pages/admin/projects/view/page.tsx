import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { projectsApi } from '../../../../lib/api/projects';
import type { Project } from '../../../../lib/api/types';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState, RichTextContent, Tag } from '../../../../components/ui';
import { localized } from '../../../../lib/i18n/localized';

const ViewProjectPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language.startsWith('en') ? 'en-CA' : 'fr-CA';
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [project, setProject] = useState<Project | null>(null);

  useEffect(() => {
    if (id) {
      loadProject();
    }
  }, [id]);

  const loadProject = async () => {
    if (!id) return;

    try {
      setLoading(true);
      const response = await projectsApi.getProjectForAdmin(id);
      setProject(response.data);
    } catch (err: any) {
      console.error('Error loading project:', err);
      setError(err.message || t('admin.projects.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!project || !id) return;

    if (!confirm(t('admin.projects.confirmDelete', { title: localized(project.title, project.titleEn, i18n.language) }))) return;

    try {
      await projectsApi.deleteProject(id);
      navigate('/admin/projects');
    } catch (err: any) {
      console.error('Error deleting project:', err);
      setError(err.message || t('admin.projects.errorDelete'));
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return '';
    return new Date(dateString).toLocaleDateString(locale);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error || !project) {
    return (
      <EmptyState
        tone="error"
        title={error || t('admin.projects.notFound')}
        action={
          <Button to="/admin/projects" variant="secondary">
            {t('admin.common.backToList')}
          </Button>
        }
      />
    );
  }

  return (
    <AdminDetailLayout
      title={localized(project.title, project.titleEn, i18n.language)}
      subtitle={`${localized(project.location, project.locationEn, i18n.language)} • ${t(`public.engagement.projets.type.${project.type}`, { defaultValue: project.type })}`}
      backPath="/admin/projects"
      status={{
        status: project.isActive ? 'published' : 'draft',
        label: project.isActive ? t('admin.common.active') : t('admin.common.inactive'),
      }}
      secondaryActions={<Tag>{t(`public.engagement.projets.status.${project.status}`, { defaultValue: project.status })}</Tag>}
      actions={
        <>
          <Button to={`/admin/projects/${project.id}/edit`} variant="secondary">
            {t('admin.common.edit')}
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            {t('admin.common.delete')}
          </Button>
        </>
      }
      main={
        <>
          {error && <div className="border border-error bg-surface px-4 py-3 text-error">{error}</div>}

          {project.imageUrl ? (
            <img
              src={project.imageUrl}
              alt={localized(project.title, project.titleEn, i18n.language)}
              className="h-64 w-full border border-line object-cover"
            />
          ) : (
            <div className="flex h-64 w-full items-center justify-center border border-line bg-surface-container">
              <div className="text-center text-ink-variant">
                <i className="ri-image-line mb-2 block text-4xl"></i>
                <div className="text-body-md">{t('admin.projects.noImage')}</div>
              </div>
            </div>
          )}

          <div className="border border-line bg-surface p-6">
            <div className="mb-2 flex items-center justify-between">
              <span className="text-label-md uppercase text-ink-variant">{t('admin.projects.colProgress')}</span>
              <span className="font-display text-headline-sm text-green">{project.progress}%</span>
            </div>
            <div className="h-3 w-full border border-line bg-surface-container">
              <div className="h-full bg-green" style={{ width: `${project.progress}%` }}></div>
            </div>
          </div>

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.common.description')}</h2>
            <RichTextContent value={localized(project.description, project.descriptionEn, i18n.language)} className="mt-3 text-body-md text-ink-variant" />
          </div>

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.projects.keyInformation')}</h2>
            <DetailList>
              <DetailRow label={t('admin.projects.totalBudget')} value={project.budget} />
              <DetailRow label={t('admin.projects.fundsRaised')} value={project.fundsRaised} />
              <DetailRow label={t('admin.projects.beneficiaries')} value={localized(project.beneficiaries, project.beneficiariesEn, i18n.language)} />
              <DetailRow label={t('admin.common.type')} value={t(`public.engagement.projets.type.${project.type}`, { defaultValue: project.type })} />
            </DetailList>
          </div>

          {(project.startDate || project.endDate) && (
            <div>
              <h2 className="font-display text-headline-sm text-green">{t('admin.projects.timeline')}</h2>
              <DetailList>
                {project.startDate && <DetailRow label={t('admin.projects.startDate')} value={formatDate(project.startDate)} />}
                {project.endDate && <DetailRow label={t('admin.projects.endDate')} value={formatDate(project.endDate)} />}
              </DetailList>
            </div>
          )}

          {project.partners.length > 0 && (
            <div>
              <h2 className="font-display text-headline-sm text-green">{t('admin.projects.partners')}</h2>
              <div className="mt-3 flex flex-wrap gap-2">
                {project.partners.map((partner, idx) => (
                  <Tag key={idx}>{partner}</Tag>
                ))}
              </div>
            </div>
          )}

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.projects.metadata')}</h2>
            <DetailList>
              <DetailRow label={t('admin.projects.createdAt')} value={formatDate(project.createdAt)} />
              <DetailRow label={t('admin.projects.updatedAt')} value={formatDate(project.updatedAt)} />
              <DetailRow label={t('admin.common.status')} value={project.isActive ? t('admin.common.active') : t('admin.common.inactive')} />
              <DetailRow label={t('admin.projects.projectId')} value={<span className="font-mono">{project.id}</span>} />
            </DetailList>
          </div>
        </>
      }
    />
  );
};

export default ViewProjectPage;
