import React, { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AdminLanguageTabs,
  isEnglishContentIncomplete,
} from '../../../../components/admin/AdminLanguageTabs';
import { AdminFormLayout } from '../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses, RichTextEditor } from '../../../../components/ui';
import { grantsApi } from '../../../../lib/api/grants';
import type { CreateGrantProgramRequest } from '../../../../lib/api/types';
import { GRANT_ICON_OPTIONS, parseCriteriaText } from '../grant-form-utils';

const GrantCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [criteriaText, setCriteriaText] = useState('');
  const [criteriaTextEn, setCriteriaTextEn] = useState('');
  const [formData, setFormData] = useState<
    Omit<CreateGrantProgramRequest, 'eligibilityCriteria' | 'eligibilityCriteriaEn'>
  >({
    title: '',
    titleEn: '',
    description: '',
    descriptionEn: '',
    icon: 'ri-graduation-cap-line',
    amount: '',
    amountEn: '',
    duration: '',
    durationEn: '',
    applicationUrl: '',
    displayOrder: 0,
    isActive: true,
  });

  const backPath = '/admin/grants';

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    const { name, value, type } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        type === 'number'
          ? parseInt(value, 10) || 0
          : type === 'checkbox'
            ? (e.target as HTMLInputElement).checked
            : value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      const response = await grantsApi.createGrant({
        ...formData,
        eligibilityCriteria: parseCriteriaText(criteriaText),
        eligibilityCriteriaEn: parseCriteriaText(criteriaTextEn),
      });
      if (response.success && response.data) {
        navigate(`/admin/grants/${response.data.id}`);
      } else {
        setError(response.message || t('admin.grants.errorCreate'));
      }
    } catch (err) {
      console.error('Error creating grant:', err);
      setError(t('admin.grants.errorCreate'));
    } finally {
      setSubmitting(false);
    }
  };

  const enIncomplete = isEnglishContentIncomplete([
    [formData.title, formData.titleEn],
    [formData.description, formData.descriptionEn],
    [formData.amount, formData.amountEn],
    [formData.duration, formData.durationEn],
    [criteriaText, criteriaTextEn],
  ]);

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title={t('admin.grants.createTitle')}
        backPath={backPath}
        backLabel={t('admin.common.backToList')}
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? t('admin.common.loading') : t('admin.common.create')}
          </Button>
        }
        languageTabs={
          <AdminLanguageTabs
            enIncomplete={enIncomplete}
            frPanel={
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <div className="md:col-span-2">
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
                <div className="md:col-span-2">
                  <Field label={t('admin.common.description')} htmlFor="description" required>
                    <RichTextEditor
                      id="description"
                      value={formData.description}
                      onChange={(description) => setFormData((current) => ({ ...current, description }))}
                      required
                      minHeight={280}
                      label={t('admin.common.description')}
                    />
                  </Field>
                </div>
                <div>
                  <Field label={t('admin.grants.colAmount')} htmlFor="amount" required>
                    <input
                      type="text"
                      id="amount"
                      name="amount"
                      value={formData.amount}
                      onChange={handleChange}
                      required
                      placeholder="Jusqu'à 15 000 $ CAD"
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div>
                  <Field label={t('admin.grants.colDuration')} htmlFor="duration" required>
                    <input
                      type="text"
                      id="duration"
                      name="duration"
                      value={formData.duration}
                      onChange={handleChange}
                      required
                      placeholder="Annuel"
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div className="md:col-span-2">
                  <Field label={t('admin.grants.criteria')} htmlFor="criteriaText" required hint={t('admin.grants.criteriaHint')}>
                    <textarea
                      id="criteriaText"
                      value={criteriaText}
                      onChange={(e) => setCriteriaText(e.target.value)}
                      required
                      rows={6}
                      placeholder={t('admin.grants.criteriaHint')}
                      className={inputClasses}
                    />
                  </Field>
                </div>
              </div>
            }
            enPanel={
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <div className="md:col-span-2">
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
                <div>
                  <Field label={t('admin.grants.colAmount')} htmlFor="amountEn">
                    <input
                      type="text"
                      id="amountEn"
                      name="amountEn"
                      value={formData.amountEn || ''}
                      onChange={handleChange}
                      placeholder="Up to CAD $15,000"
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div>
                  <Field label={t('admin.grants.colDuration')} htmlFor="durationEn">
                    <input
                      type="text"
                      id="durationEn"
                      name="durationEn"
                      value={formData.durationEn || ''}
                      onChange={handleChange}
                      placeholder="Annual"
                      className={inputClasses}
                    />
                  </Field>
                </div>
                <div className="md:col-span-2">
                  <Field label={t('admin.grants.criteria')} htmlFor="criteriaTextEn">
                    <textarea
                      id="criteriaTextEn"
                      value={criteriaTextEn}
                      onChange={(e) => setCriteriaTextEn(e.target.value)}
                      rows={6}
                      placeholder={t('admin.grants.criteriaHint')}
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
              <Field label={t('admin.grants.icon')} htmlFor="icon" required>
                <select
                  id="icon"
                  name="icon"
                  value={formData.icon}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  {GRANT_ICON_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label={t('admin.grants.colOrder')} htmlFor="displayOrder">
                <input
                  type="number"
                  id="displayOrder"
                  name="displayOrder"
                  value={formData.displayOrder}
                  onChange={handleChange}
                  min={0}
                  className={inputClasses}
                />
              </Field>
              <div className="md:col-span-2">
                <Field label={t('admin.grants.applicationUrl')} htmlFor="applicationUrl">
                  <input
                    type="url"
                    id="applicationUrl"
                    name="applicationUrl"
                    value={formData.applicationUrl}
                    onChange={handleChange}
                    placeholder="https://..."
                    className={inputClasses}
                  />
                </Field>
              </div>
              <div className="md:col-span-2">
                <label htmlFor="isActive" className="flex min-h-[44px] cursor-pointer items-center gap-3">
                  <input
                    type="checkbox"
                    id="isActive"
                    name="isActive"
                    checked={formData.isActive}
                    onChange={handleChange}
                    className="h-5 w-5 rounded-control-sm border border-outline accent-green"
                  />
                  <span className="text-body-md text-ink">{t('admin.common.active')}</span>
                </label>
              </div>
            </div>
          </div>
        }
      />
    </form>
  );
};

export default GrantCreatePage;
