import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AdminLanguageTabs,
  isEnglishContentIncomplete,
} from '../../../../components/admin/AdminLanguageTabs';
import { AdminFormLayout } from '../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses, RichTextEditor } from '../../../../components/ui';
import { projectsApi } from '../../../../lib/api/projects';
import type { Project, UpdateProjectRequest } from '../../../../lib/api/types';

const EditProjectPage = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const [loading, setLoading] = useState(false);
  const [loadingProject, setLoadingProject] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [project, setProject] = useState<Project | null>(null);
  const [formData, setFormData] = useState<UpdateProjectRequest>({});
  const [partners, setPartners] = useState<string>('');

  const backPath = '/admin/projects';

  useEffect(() => {
    if (id) {
      loadProject();
    }
  }, [id]);

  const loadProject = async () => {
    if (!id) return;

    try {
      setLoadingProject(true);
      const response = await projectsApi.getProjectForAdmin(id);
      const projectData = response.data;
      setProject(projectData);

      setFormData({
        title: projectData.title,
        titleEn: projectData.titleEn || '',
        location: projectData.location,
        locationEn: projectData.locationEn || '',
        type: projectData.type,
        status: projectData.status,
        progress: projectData.progress,
        description: projectData.description,
        descriptionEn: projectData.descriptionEn || '',
        imageUrl: projectData.imageUrl,
        budget: projectData.budget,
        fundsRaised: projectData.fundsRaised,
        beneficiaries: projectData.beneficiaries,
        beneficiariesEn: projectData.beneficiariesEn || '',
        startDate: projectData.startDate ? projectData.startDate.split('T')[0] : '',
        endDate: projectData.endDate ? projectData.endDate.split('T')[0] : '',
        isActive: projectData.isActive,
      });

      setPartners(projectData.partners.join(', '));
    } catch (err: any) {
      console.error('Error loading project:', err);
      setError(err.message || 'Failed to load project');
    } finally {
      setLoadingProject(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;

    setLoading(true);
    setError(null);

    try {
      const requestData = {
        ...formData,
        startDate: formData.startDate || null,
        endDate: formData.endDate || null,
        partners: partners
          .split(',')
          .map((p) => p.trim())
          .filter((p) => p.length > 0),
      };

      await projectsApi.updateProject(id, requestData);
      navigate(backPath);
    } catch (err: any) {
      console.error('Error updating project:', err);
      setError(err.message || 'Failed to update project');
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    const { name, value, type } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        type === 'number'
          ? parseInt(value) || 0
          : type === 'checkbox'
            ? (e.target as HTMLInputElement).checked
            : value,
    }));
  };

  const enIncomplete = isEnglishContentIncomplete([
    [formData.title, formData.titleEn],
    [formData.location, formData.locationEn],
    [formData.description, formData.descriptionEn],
    [formData.beneficiaries, formData.beneficiariesEn],
  ]);

  if (loadingProject) {
    return (
      <div className="flex h-64 items-center justify-center">
        <div className="text-ink-variant">{t('admin.projects.loading')}</div>
      </div>
    );
  }

  if (!project) {
    return (
      <div className="border border-error bg-surface px-6 py-12 text-center">
        <p className="text-error">{t('admin.projects.notFound')}</p>
        <button
          onClick={() => navigate(backPath)}
          className="mt-2 text-body-md text-red-link underline hover:text-green"
        >
          {t('admin.common.backToList')}
        </button>
      </div>
    );
  }

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title={t('admin.common.edit')}
        backPath={backPath}
        backLabel={t('admin.common.backToList')}
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={loading}>
            {loading ? t('admin.common.loading') : t('admin.common.save')}
          </Button>
        }
        languageTabs={
          <AdminLanguageTabs
            enIncomplete={enIncomplete}
            frPanel={
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <div>
                  <Field label={t('admin.common.title')} htmlFor="title">
                    <input
                      type="text"
                      id="title"
                      name="title"
                      value={formData.title || ''}
                      onChange={handleChange}
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div>
                  <Field label={t('admin.common.location')} htmlFor="location">
                    <input
                      type="text"
                      id="location"
                      name="location"
                      value={formData.location || ''}
                      onChange={handleChange}
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div>
                  <Field label="Beneficiaries" htmlFor="beneficiaries">
                    <input
                      type="text"
                      id="beneficiaries"
                      name="beneficiaries"
                      value={formData.beneficiaries || ''}
                      onChange={handleChange}
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div className="md:col-span-2">
                  <Field label={t('admin.common.description')} htmlFor="description">
                    <RichTextEditor
                      id="description"
                      value={formData.description || ''}
                      onChange={(description) => setFormData((current) => ({ ...current, description }))}
                      minHeight={280}
                      label={t('admin.common.description')}
                    />
                  </Field>
                </div>
              </div>
            }
            enPanel={
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <div>
                  <Field label={t('admin.common.title')} htmlFor="titleEn">
                    <input
                      type="text"
                      id="titleEn"
                      name="titleEn"
                      value={formData.titleEn || ''}
                      onChange={handleChange}
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div>
                  <Field label={t('admin.common.location')} htmlFor="locationEn">
                    <input
                      type="text"
                      id="locationEn"
                      name="locationEn"
                      value={formData.locationEn || ''}
                      onChange={handleChange}
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div>
                  <Field label="Beneficiaries" htmlFor="beneficiariesEn">
                    <input
                      type="text"
                      id="beneficiariesEn"
                      name="beneficiariesEn"
                      value={formData.beneficiariesEn || ''}
                      onChange={handleChange}
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div className="md:col-span-2">
                  <Field label={t('admin.common.description')} htmlFor="descriptionEn">
                    <RichTextEditor
                      id="descriptionEn"
                      value={formData.descriptionEn || ''}
                      onChange={(descriptionEn) => setFormData((current) => ({ ...current, descriptionEn }))}
                      minHeight={280}
                      label={t('admin.common.description')}
                    />
                  </Field>
                </div>
              </div>
            }
          />
        }
        main={
          <div>
            {error && (
              <p className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</p>
            )}
            <h2 className="mb-4 border-b border-line pb-3 text-label-md uppercase text-ink-variant">
              {t('admin.content.lang.settings')}
            </h2>
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
              <Field label={t('admin.common.type')} htmlFor="type">
                <select
                  id="type"
                  name="type"
                  value={formData.type || ''}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="Développement au Burkina">{t('public.engagement.projets.type.Développement au Burkina')}</option>
                  <option value="Initiative Locale">{t('public.engagement.projets.type.Initiative Locale')}</option>
                </select>
              </Field>
              <Field label={t('admin.common.status')} htmlFor="status">
                <select
                  id="status"
                  name="status"
                  value={formData.status || ''}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="Planification">{t('public.engagement.projets.status.Planification')}</option>
                  <option value="En cours">{t('public.engagement.projets.status.En cours')}</option>
                  <option value="Actif">{t('public.engagement.projets.status.Actif')}</option>
                  <option value="Terminé">{t('public.engagement.projets.status.Terminé')}</option>
                </select>
              </Field>
              <Field label={`${t('admin.projects.colProgress')} (0-100)`} htmlFor="progress">
                <input
                  type="number"
                  id="progress"
                  name="progress"
                  value={formData.progress || 0}
                  onChange={handleChange}
                  min="0"
                  max="100"
                  className={inputClasses}
                />
              </Field>
              <Field label="Image URL" htmlFor="imageUrl">
                <input
                  type="url"
                  id="imageUrl"
                  name="imageUrl"
                  value={formData.imageUrl || ''}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <Field label={t('admin.projects.colBudget')} htmlFor="budget">
                <input
                  type="text"
                  id="budget"
                  name="budget"
                  value={formData.budget || ''}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <Field label="Funds Raised" htmlFor="fundsRaised">
                <input
                  type="text"
                  id="fundsRaised"
                  name="fundsRaised"
                  value={formData.fundsRaised || ''}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <Field label="Start Date" htmlFor="startDate">
                <input
                  type="date"
                  id="startDate"
                  name="startDate"
                  value={formData.startDate || ''}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <Field label="End Date" htmlFor="endDate">
                <input
                  type="date"
                  id="endDate"
                  name="endDate"
                  value={formData.endDate || ''}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <div className="md:col-span-2">
                <label htmlFor="isActive" className="flex min-h-[44px] cursor-pointer items-center gap-3">
                  <input
                    type="checkbox"
                    id="isActive"
                    name="isActive"
                    checked={formData.isActive ?? true}
                    onChange={handleChange}
                    className="h-5 w-5 rounded-control-sm border border-outline accent-green"
                  />
                  <span className="text-body-md text-ink">{t('admin.common.active')}</span>
                </label>
              </div>
              <div className="md:col-span-2">
                <Field label="Partners (comma-separated)" htmlFor="partners">
                  <input
                    type="text"
                    id="partners"
                    value={partners}
                    onChange={(e) => setPartners(e.target.value)}
                    className={inputClasses}
                  />
                </Field>
              </div>
            </div>
          </div>
        }
      />
    </form>
  );
};

export default EditProjectPage;
