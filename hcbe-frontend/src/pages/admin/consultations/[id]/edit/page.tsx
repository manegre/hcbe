import React, { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AdminLanguageTabs,
  isEnglishContentIncomplete,
} from '../../../../../components/admin/AdminLanguageTabs';
import { AdminFormLayout } from '../../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../../components/ui';
import { consultationsApi } from '../../../../../lib/api/consultations';
import type { UpdateConsultationRequest } from '../../../../../lib/api/types';
import {
  CONSULTATION_ACCENT_OPTIONS,
  CONSULTATION_ICON_OPTIONS,
  CONSULTATION_LAYOUT_OPTIONS,
} from '../../consultation-form-utils';

const ConsultationEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<
    UpdateConsultationRequest & {
      title: string;
      description: string;
      icon: string;
      layoutType: 'featured' | 'card';
      accentColor: 'emerald' | 'amber';
    }
  >({
    title: '',
    titleEn: '',
    description: '',
    descriptionEn: '',
    icon: 'ri-chat-poll-line',
    layoutType: 'card',
    actionUrl: '',
    actionLabel: '',
    actionLabelEn: '',
    secondaryActionUrl: '',
    secondaryActionLabel: '',
    secondaryActionLabelEn: '',
    accentColor: 'emerald',
    displayOrder: 0,
    isActive: true,
  });

  const backPath = `/admin/consultations/${id}`;

  useEffect(() => {
    const loadItem = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const response = await consultationsApi.getConsultationForAdmin(id);
        if (response.success && response.data) {
          const item = response.data;
          setFormData({
            title: item.title,
            titleEn: item.titleEn || '',
            description: item.description,
            descriptionEn: item.descriptionEn || '',
            icon: item.icon,
            layoutType: item.layoutType,
            actionUrl: item.actionUrl || '',
            actionLabel: item.actionLabel || '',
            actionLabelEn: item.actionLabelEn || '',
            secondaryActionUrl: item.secondaryActionUrl || '',
            secondaryActionLabel: item.secondaryActionLabel || '',
            secondaryActionLabelEn: item.secondaryActionLabelEn || '',
            accentColor: item.accentColor,
            displayOrder: item.displayOrder,
            isActive: item.isActive,
          });
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
      const response = await consultationsApi.updateConsultation(id, formData);
      if (response.success && response.data) {
        navigate(`/admin/consultations/${id}`);
      } else {
        setError(response.message || t('admin.consultations.errorUpdate'));
      }
    } catch (err) {
      console.error('Error updating consultation:', err);
      setError(t('admin.consultations.errorUpdate'));
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
    [formData.actionLabel, formData.actionLabelEn],
    [formData.secondaryActionLabel, formData.secondaryActionLabelEn],
  ]);

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title={t('admin.consultations.editTitle')}
        backPath={backPath}
        backLabel={t('admin.common.backToList')}
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
                  <Field label={t('admin.consultations.actionLabel')} htmlFor="actionLabel">
                    <input
                      type="text"
                      id="actionLabel"
                      name="actionLabel"
                      value={formData.actionLabel}
                      onChange={handleChange}
                      className={inputClasses}
                    />
                  </Field>
                </div>
                {formData.layoutType === 'featured' && (
                  <div>
                    <Field label={t('admin.consultations.secondaryActionLabel')} htmlFor="secondaryActionLabel">
                      <input
                        type="text"
                        id="secondaryActionLabel"
                        name="secondaryActionLabel"
                        value={formData.secondaryActionLabel}
                        onChange={handleChange}
                        className={inputClasses}
                      />
                    </Field>
                  </div>
                )}
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
                  <Field label={t('admin.consultations.actionLabel')} htmlFor="actionLabelEn">
                    <input
                      type="text"
                      id="actionLabelEn"
                      name="actionLabelEn"
                      value={formData.actionLabelEn || ''}
                      onChange={handleChange}
                      className={inputClasses}
                    />
                  </Field>
                </div>
                {formData.layoutType === 'featured' && (
                  <div>
                    <Field label={t('admin.consultations.secondaryActionLabel')} htmlFor="secondaryActionLabelEn">
                      <input
                        type="text"
                        id="secondaryActionLabelEn"
                        name="secondaryActionLabelEn"
                        value={formData.secondaryActionLabelEn || ''}
                        onChange={handleChange}
                        className={inputClasses}
                      />
                    </Field>
                  </div>
                )}
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
              <Field label={t('admin.consultations.icon')} htmlFor="icon">
                <select
                  id="icon"
                  name="icon"
                  value={formData.icon}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  {CONSULTATION_ICON_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label={t('admin.consultations.colLayout')} htmlFor="layoutType">
                <select
                  id="layoutType"
                  name="layoutType"
                  value={formData.layoutType}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  {CONSULTATION_LAYOUT_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {t(option.labelKey)}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label={t('admin.consultations.accentColor')} htmlFor="accentColor">
                <select
                  id="accentColor"
                  name="accentColor"
                  value={formData.accentColor}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  {CONSULTATION_ACCENT_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {t(option.labelKey)}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label={t('admin.consultations.colOrder')} htmlFor="displayOrder">
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
              <Field label={t('admin.consultations.actionUrl')} htmlFor="actionUrl">
                <input
                  type="text"
                  id="actionUrl"
                  name="actionUrl"
                  value={formData.actionUrl}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              {formData.layoutType === 'featured' && (
                <Field label={t('admin.consultations.secondaryActionUrl')} htmlFor="secondaryActionUrl">
                  <input
                    type="text"
                    id="secondaryActionUrl"
                    name="secondaryActionUrl"
                    value={formData.secondaryActionUrl}
                    onChange={handleChange}
                    className={inputClasses}
                  />
                </Field>
              )}
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

export default ConsultationEditPage;
