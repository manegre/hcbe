import React, { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AdminLanguageTabs,
  isEnglishContentIncomplete,
} from '../../../../components/admin/AdminLanguageTabs';
import { AdminFormLayout } from '../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../components/ui';
import { projectsApi } from '../../../../lib/api/projects';
import type { CreateProjectRequest } from '../../../../lib/api/types';

const CreateProjectPage = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<CreateProjectRequest>({
    title: '',
    titleEn: '',
    location: '',
    locationEn: '',
    type: 'Développement au Burkina',
    status: 'Planification',
    progress: 0,
    description: '',
    descriptionEn: '',
    imageUrl: '',
    budget: '',
    fundsRaised: '',
    beneficiaries: '',
    beneficiariesEn: '',
    startDate: '',
    endDate: '',
    partners: [],
  });
  const [partners, setPartners] = useState<string>('');

  const backPath = '/admin/projects';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
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

      await projectsApi.createProject(requestData);
      navigate(backPath);
    } catch (err: any) {
      console.error('Error creating project:', err);
      setError(err.message || 'Failed to create project');
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'progress' ? parseInt(value) || 0 : value,
    }));
  };

  const enIncomplete = isEnglishContentIncomplete([
    [formData.title, formData.titleEn],
    [formData.location, formData.locationEn],
    [formData.description, formData.descriptionEn],
    [formData.beneficiaries, formData.beneficiariesEn],
  ]);

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title={t('admin.projects.create')}
        backPath={backPath}
        backLabel={t('admin.common.backToList')}
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={loading}>
            {loading ? t('admin.common.loading') : t('admin.projects.create')}
          </Button>
        }
        languageTabs={
          <AdminLanguageTabs
            enIncomplete={enIncomplete}
            frPanel={
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <div>
                  <Field label={t('admin.common.title')} htmlFor="title" required>
                    <input
                      type="text"
                      id="title"
                      name="title"
                      value={formData.title}
                      onChange={handleChange}
                      required
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div>
                  <Field label={t('admin.common.location')} htmlFor="location" required>
                    <input
                      type="text"
                      id="location"
                      name="location"
                      value={formData.location}
                      onChange={handleChange}
                      required
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div>
                  <Field label="Beneficiaries" htmlFor="beneficiaries" required>
                    <input
                      type="text"
                      id="beneficiaries"
                      name="beneficiaries"
                      value={formData.beneficiaries}
                      onChange={handleChange}
                      required
                      placeholder="e.g., 500+ familles"
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div className="md:col-span-2">
                  <Field label={t('admin.common.description')} htmlFor="description" required>
                    <textarea
                      id="description"
                      name="description"
                      value={formData.description}
                      onChange={handleChange}
                      required
                      rows={4}
                      className={inputClasses}
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
                    <textarea
                      id="descriptionEn"
                      name="descriptionEn"
                      value={formData.descriptionEn || ''}
                      onChange={handleChange}
                      rows={4}
                      className={inputClasses}
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
              <Field label={t('admin.common.type')} htmlFor="type" required>
                <select
                  id="type"
                  name="type"
                  value={formData.type}
                  onChange={handleChange}
                  required
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="Développement au Burkina">Développement au Burkina</option>
                  <option value="Initiative Locale">Initiative Locale</option>
                </select>
              </Field>
              <Field label={t('admin.common.status')} htmlFor="status" required>
                <select
                  id="status"
                  name="status"
                  value={formData.status}
                  onChange={handleChange}
                  required
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="Planification">Planification</option>
                  <option value="En cours">En cours</option>
                  <option value="Actif">Actif</option>
                  <option value="Terminé">Terminé</option>
                </select>
              </Field>
              <Field label={`${t('admin.projects.colProgress')} (0-100)`} htmlFor="progress" required>
                <input
                  type="number"
                  id="progress"
                  name="progress"
                  value={formData.progress}
                  onChange={handleChange}
                  min="0"
                  max="100"
                  required
                  className={inputClasses}
                />
              </Field>
              <Field label="Image URL" htmlFor="imageUrl">
                <input
                  type="url"
                  id="imageUrl"
                  name="imageUrl"
                  value={formData.imageUrl}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <Field label={t('admin.projects.colBudget')} htmlFor="budget" required>
                <input
                  type="text"
                  id="budget"
                  name="budget"
                  value={formData.budget}
                  onChange={handleChange}
                  required
                  placeholder="e.g., 50,000 CAD"
                  className={inputClasses}
                />
              </Field>
              <Field label="Funds Raised" htmlFor="fundsRaised" required>
                <input
                  type="text"
                  id="fundsRaised"
                  name="fundsRaised"
                  value={formData.fundsRaised}
                  onChange={handleChange}
                  required
                  placeholder="e.g., 25,000 CAD"
                  className={inputClasses}
                />
              </Field>
              <Field label="Start Date" htmlFor="startDate">
                <input
                  type="date"
                  id="startDate"
                  name="startDate"
                  value={formData.startDate}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <Field label="End Date" htmlFor="endDate">
                <input
                  type="date"
                  id="endDate"
                  name="endDate"
                  value={formData.endDate}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <div className="md:col-span-2">
                <Field label="Partners (comma-separated)" htmlFor="partners">
                  <input
                    type="text"
                    id="partners"
                    value={partners}
                    onChange={(e) => setPartners(e.target.value)}
                    placeholder="e.g., UNICEF, Croix-Rouge, Government of Canada"
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

export default CreateProjectPage;
