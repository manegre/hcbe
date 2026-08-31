import React, { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AdminLanguageTabs,
  isEnglishContentIncomplete,
} from '../../../../../components/admin/AdminLanguageTabs';
import { AdminFormLayout } from '../../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../../components/ui';
import { grantsApi } from '../../../../../lib/api/grants';
import type { UpdateGrantProgramRequest } from '../../../../../lib/api/types';
import { GRANT_ICON_OPTIONS, formatCriteriaText, parseCriteriaText } from '../../grant-form-utils';

const GrantEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [criteriaText, setCriteriaText] = useState('');
  const [criteriaTextEn, setCriteriaTextEn] = useState('');
  const [formData, setFormData] = useState<
    UpdateGrantProgramRequest & {
      title: string;
      description: string;
      icon: string;
      amount: string;
      duration: string;
    }
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

  const backPath = `/admin/grants/${id}`;

  useEffect(() => {
    const loadGrant = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const response = await grantsApi.getGrantForAdmin(id);
        if (response.success && response.data) {
          const grant = response.data;
          setFormData({
            title: grant.title,
            titleEn: grant.titleEn || '',
            description: grant.description,
            descriptionEn: grant.descriptionEn || '',
            icon: grant.icon,
            amount: grant.amount,
            amountEn: grant.amountEn || '',
            duration: grant.duration,
            durationEn: grant.durationEn || '',
            applicationUrl: grant.applicationUrl || '',
            displayOrder: grant.displayOrder,
            isActive: grant.isActive,
          });
          setCriteriaText(formatCriteriaText(grant.eligibilityCriteria));
          setCriteriaTextEn(formatCriteriaText(grant.eligibilityCriteriaEn || []));
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
    if (!id) return;

    setSubmitting(true);
    setError(null);

    try {
      const response = await grantsApi.updateGrant(id, {
        ...formData,
        eligibilityCriteria: parseCriteriaText(criteriaText),
        eligibilityCriteriaEn: parseCriteriaText(criteriaTextEn),
      });
      if (response.success) {
        navigate(`/admin/grants/${id}`);
      } else {
        setError(response.message || t('admin.grants.errorUpdate'));
      }
    } catch (err) {
      console.error('Error updating grant:', err);
      setError(t('admin.grants.errorUpdate'));
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

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
        title={t('admin.grants.editTitle')}
        backPath={backPath}
        backLabel={t('admin.common.back')}
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? t('admin.common.loading') : t('admin.common.save')}
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
                <div>
                  <Field label={t('admin.grants.colAmount')} htmlFor="amount" required>
                    <input
                      type="text"
                      id="amount"
                      name="amount"
                      value={formData.amount}
                      onChange={handleChange}
                      required
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
                <div>
                  <Field label={t('admin.grants.colAmount')} htmlFor="amountEn">
                    <input
                      type="text"
                      id="amountEn"
                      name="amountEn"
                      value={formData.amountEn || ''}
                      onChange={handleChange}
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

export default GrantEditPage;
