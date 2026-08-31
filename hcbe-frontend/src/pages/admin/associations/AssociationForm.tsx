import React, { useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { resolveMediaUrl } from '../../../lib/api/media-url';
import { AdminFormLayout } from '../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../components/ui';

export const CANADA_PROVINCES = [
  'Alberta',
  'Colombie-Britannique',
  'Manitoba',
  'Nouveau-Brunswick',
  'Terre-Neuve-et-Labrador',
  'Territoires du Nord-Ouest',
  'Nouvelle-Écosse',
  'Nunavut',
  'Ontario',
  'Île-du-Prince-Édouard',
  'Québec',
  'Saskatchewan',
  'Yukon',
] as const;

export interface AssociationFormValues {
  name: string;
  nameEn: string;
  description: string;
  descriptionEn: string;
  province: string;
  city: string;
  contact: string;
  phone: string;
  president: string;
  memberCount: string;
  foundedYear?: number;
  imageUrl: string;
  website: string;
  domains: string[];
  domainsEn: string[];
  isActive?: boolean;
}

interface AssociationFormProps {
  formData: AssociationFormValues;
  onChange: (data: AssociationFormValues) => void;
  onSubmit: (e: React.FormEvent) => void;
  submitting: boolean;
  submitLabel: string;
  submittingLabel: string;
  onCancel: () => void;
  showActiveToggle?: boolean;
  imageFile: File | null;
  onImageFileChange: (file: File | null) => void;
}

export const AssociationForm: React.FC<AssociationFormProps> = ({
  formData,
  onChange,
  onSubmit,
  submitting,
  submitLabel,
  submittingLabel,
  onCancel,
  showActiveToggle = false,
  imageFile,
  onImageFileChange,
}) => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const [domainInput, setDomainInput] = useState('');
  const [domainInputEn, setDomainInputEn] = useState('');
  const formRef = useRef<HTMLFormElement>(null);
  const imageInputRef = useRef<HTMLInputElement>(null);
  const [imagePreviewUrl, setImagePreviewUrl] = useState('');
  const hasImage = Boolean(imageFile || formData.imageUrl);

  const initialSnapshotRef = useRef(JSON.stringify(formData));
  const isDirty = JSON.stringify(formData) !== initialSnapshotRef.current;

  const backPath = id ? `/admin/associations/${id}` : '/admin/associations';
  const backLabel = id ? t('admin.common.back') : t('admin.common.backToList');
  const title = id ? t('admin.associations.editTitle') : t('admin.associations.createTitle');

  useEffect(() => {
    if (!imageFile) {
      setImagePreviewUrl(resolveMediaUrl(formData.imageUrl));
      return;
    }

    const objectUrl = URL.createObjectURL(imageFile);
    setImagePreviewUrl(objectUrl);
    return () => URL.revokeObjectURL(objectUrl);
  }, [imageFile, formData.imageUrl]);

  const updateField = <K extends keyof AssociationFormValues>(
    field: K,
    value: AssociationFormValues[K],
  ) => {
    onChange({ ...formData, [field]: value });
  };

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    const { name, value, type } = e.target;
    if (type === 'checkbox') {
      updateField('isActive', (e.target as HTMLInputElement).checked);
      return;
    }
    if (name === 'foundedYear') {
      updateField('foundedYear', value ? parseInt(value, 10) : undefined);
      return;
    }
    updateField(name as keyof AssociationFormValues, value as never);
  };

  const handleAddDomain = () => {
    const domain = domainInput.trim();
    if (!domain || formData.domains.includes(domain)) return;
    updateField('domains', [...formData.domains, domain]);
    setDomainInput('');
  };

  const handleRemoveDomain = (domain: string) => {
    updateField(
      'domains',
      formData.domains.filter((item) => item !== domain),
    );
  };

  const handleAddDomainEn = () => {
    const domain = domainInputEn.trim();
    if (!domain || formData.domainsEn.includes(domain)) return;
    updateField('domainsEn', [...formData.domainsEn, domain]);
    setDomainInputEn('');
  };

  const handleRemoveDomainEn = (domain: string) => {
    updateField('domainsEn', formData.domainsEn.filter((item) => item !== domain));
  };

  const handleDomainKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleAddDomain();
    }
  };

  const handleImageFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    onImageFileChange(file);
    if (file) {
      updateField('imageUrl', '');
    }
  };

  const handleImageUrlChange = (value: string) => {
    if (value.trim()) {
      onImageFileChange(null);
      if (imageInputRef.current) imageInputRef.current.value = '';
    }
    updateField('imageUrl', value);
  };

  const handleRemoveImage = () => {
    onImageFileChange(null);
    updateField('imageUrl', '');
    if (imageInputRef.current) imageInputRef.current.value = '';
  };

  return (
    <form ref={formRef} onSubmit={onSubmit} className="min-w-0">
      <AdminFormLayout
        title={title}
        backPath={backPath}
        backLabel={backLabel}
        isDirty={isDirty}
        dirtyLabel={t('admin.common.unsavedChanges')}
        onCancel={onCancel}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? submittingLabel : submitLabel}
          </Button>
        }
        main={
          <>
            <div>
              <h2 className="mb-4 border-b border-line pb-3 text-label-md uppercase text-ink-variant">
                {t('admin.associations.sectionBasic')}
              </h2>
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <div className="md:col-span-2">
                  <Field label={t('admin.associations.name')} htmlFor="name" required>
                    <input
                      type="text"
                      id="name"
                      name="name"
                      value={formData.name}
                      onChange={handleInputChange}
                      required
                      className={inputClasses}
                      placeholder={t('admin.associations.namePlaceholder')}
                    />
                  </Field>
                </div>

                <div className="md:col-span-2">
                  <Field label={t('admin.associations.nameEn')} htmlFor="nameEn">
                    <input type="text" id="nameEn" name="nameEn" value={formData.nameEn} onChange={handleInputChange} className={inputClasses} placeholder={t('admin.associations.nameEnPlaceholder')} />
                  </Field>
                </div>

                <div className="md:col-span-2">
                  <Field label={t('admin.associations.description')} htmlFor="description">
                    <textarea
                      id="description"
                      name="description"
                      value={formData.description}
                      onChange={handleInputChange}
                      rows={3}
                      className={inputClasses}
                      placeholder={t('admin.associations.descriptionPlaceholder')}
                    />
                  </Field>
                </div>

                <div className="md:col-span-2">
                  <Field label={t('admin.associations.descriptionEn')} htmlFor="descriptionEn">
                    <textarea id="descriptionEn" name="descriptionEn" value={formData.descriptionEn} onChange={handleInputChange} rows={3} className={inputClasses} placeholder={t('admin.associations.descriptionEnPlaceholder')} />
                  </Field>
                </div>

                <Field label={t('admin.associations.province')} htmlFor="province" required>
                  <select
                    id="province"
                    name="province"
                    value={formData.province}
                    onChange={handleInputChange}
                    required
                    className={`${inputClasses} cursor-pointer`}
                  >
                    <option value="">{t('admin.associations.selectProvince')}</option>
                    {CANADA_PROVINCES.map((province) => (
                      <option key={province} value={province}>
                        {province}
                      </option>
                    ))}
                  </select>
                </Field>

                <Field label={t('admin.associations.city')} htmlFor="city" required>
                  <input
                    type="text"
                    id="city"
                    name="city"
                    value={formData.city}
                    onChange={handleInputChange}
                    required
                    className={inputClasses}
                    placeholder={t('admin.associations.cityPlaceholder')}
                  />
                </Field>

                {showActiveToggle && (
                  <div className="md:col-span-2">
                    <label htmlFor="isActive" className="flex min-h-[44px] cursor-pointer items-center gap-3">
                      <input
                        type="checkbox"
                        id="isActive"
                        name="isActive"
                        checked={formData.isActive ?? false}
                        onChange={handleInputChange}
                        className="h-5 w-5 rounded-control-sm border border-outline accent-green"
                      />
                      <span className="text-body-md text-ink">{t('admin.associations.isActive')}</span>
                    </label>
                  </div>
                )}
              </div>
            </div>

            <div>
              <h2 className="mb-4 border-b border-line pb-3 text-label-md uppercase text-ink-variant">
                {t('admin.associations.sectionContact')}
              </h2>
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <Field label={t('admin.associations.contactEmail')} htmlFor="contact">
                  <input
                    type="email"
                    id="contact"
                    name="contact"
                    value={formData.contact}
                    onChange={handleInputChange}
                    className={inputClasses}
                    placeholder="contact@association.ca"
                  />
                </Field>
                <Field label={t('admin.associations.website')} htmlFor="website">
                  <input
                    type="url"
                    id="website"
                    name="website"
                    value={formData.website}
                    onChange={handleInputChange}
                    className={inputClasses}
                    placeholder="https://www.association.ca"
                  />
                </Field>
              </div>
            </div>

            <div>
              <h2 className="mb-4 border-b border-line pb-3 text-label-md uppercase text-ink-variant">
                {t('admin.associations.sectionLeadership')}
              </h2>
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <Field label={t('admin.associations.president')} htmlFor="president">
                  <input
                    type="text"
                    id="president"
                    name="president"
                    value={formData.president}
                    onChange={handleInputChange}
                    className={inputClasses}
                    placeholder={t('admin.associations.presidentPlaceholder')}
                  />
                </Field>
                <Field label={t('admin.associations.phone')} htmlFor="phone">
                  <input
                    type="tel"
                    id="phone"
                    name="phone"
                    value={formData.phone}
                    onChange={handleInputChange}
                    className={inputClasses}
                    placeholder={t('admin.associations.phonePlaceholder')}
                  />
                </Field>
                <Field label={t('admin.associations.memberCount')} htmlFor="memberCount">
                  <input
                    type="text"
                    id="memberCount"
                    name="memberCount"
                    value={formData.memberCount}
                    onChange={handleInputChange}
                    className={inputClasses}
                    placeholder={t('admin.associations.memberCountPlaceholder')}
                  />
                </Field>
                <Field label={t('admin.associations.foundedYear')} htmlFor="foundedYear">
                  <input
                    type="number"
                    id="foundedYear"
                    name="foundedYear"
                    value={formData.foundedYear ?? ''}
                    onChange={handleInputChange}
                    min={1900}
                    max={new Date().getFullYear()}
                    className={inputClasses}
                    placeholder={t('admin.associations.foundedYearPlaceholder')}
                  />
                </Field>
              </div>
            </div>

            <div>
              <h2 className="mb-4 border-b border-line pb-3 text-label-md uppercase text-ink-variant">
                {t('admin.associations.sectionDomains')}
                <span className="ml-1 text-red-link">*</span>
              </h2>
              <div className="space-y-4">
                <div className="flex flex-wrap items-end gap-2">
                  <div className="min-w-[220px] flex-1">
                    <Field label={t('admin.associations.domainLabel')} htmlFor="domainInput">
                      <input
                        type="text"
                        id="domainInput"
                        name="domainInput"
                        value={domainInput}
                        onChange={(e) => setDomainInput(e.target.value)}
                        onKeyDown={handleDomainKeyDown}
                        className={inputClasses}
                        placeholder={t('admin.associations.domainPlaceholder')}
                      />
                    </Field>
                  </div>
                  <Button type="button" variant="secondary" onClick={handleAddDomain}>
                    {t('admin.associations.addDomain')}
                  </Button>
                </div>
                {formData.domains.length > 0 && (
                  <div className="flex flex-wrap gap-2">
                    {formData.domains.map((domain) => (
                      <span
                        key={domain}
                        className="inline-flex items-center gap-2 border border-line px-3 py-1 text-body-md text-ink-variant"
                      >
                        {domain}
                        <button
                          type="button"
                          onClick={() => handleRemoveDomain(domain)}
                          className="text-ink-variant hover:text-red-link"
                          aria-label={t('admin.common.delete')}
                        >
                          <i className="ri-close-line" aria-hidden="true"></i>
                        </button>
                      </span>
                    ))}
                  </div>
                )}
                <div className="border-t border-line pt-4">
                  <div className="flex flex-wrap items-end gap-2">
                    <div className="min-w-[220px] flex-1">
                      <Field label={t('admin.associations.domainLabelEn')} htmlFor="domainInputEn">
                        <input type="text" id="domainInputEn" value={domainInputEn} onChange={(e) => setDomainInputEn(e.target.value)} onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); handleAddDomainEn(); } }} className={inputClasses} placeholder={t('admin.associations.domainPlaceholderEn')} />
                      </Field>
                    </div>
                    <Button type="button" variant="secondary" onClick={handleAddDomainEn}>{t('admin.associations.addDomain')}</Button>
                  </div>
                  {formData.domainsEn.length > 0 && (
                    <div className="mt-3 flex flex-wrap gap-2">
                      {formData.domainsEn.map((domain) => (
                        <span key={domain} className="inline-flex items-center gap-2 border border-line px-3 py-1 text-body-md text-ink-variant">
                          {domain}
                          <button type="button" onClick={() => handleRemoveDomainEn(domain)} className="text-ink-variant hover:text-red-link" aria-label={t('admin.common.delete')}><i className="ri-close-line" aria-hidden="true" /></button>
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </div>
          </>
        }
        aside={
          <div className="border border-line bg-surface p-6">
            <h2 className="mb-1 text-label-md uppercase text-ink-variant">{t('admin.associations.sectionImage')}</h2>
            <p className="mb-4 text-body-md text-ink-variant">{t('admin.associations.imageHint')}</p>

            {imagePreviewUrl ? (
              <img
                src={imagePreviewUrl}
                alt={t('admin.associations.imagePreview')}
                className="mb-4 aspect-square w-full border border-line object-cover"
              />
            ) : (
              <div className="mb-4 flex aspect-square w-full items-center justify-center border border-line bg-surface-container text-ink-variant">
                <i className="ri-image-add-line text-4xl" aria-hidden="true"></i>
              </div>
            )}

            <div className="flex flex-wrap items-center gap-2">
              <Button type="button" variant="secondary" onClick={() => imageInputRef.current?.click()} disabled={submitting}>
                {t('admin.associations.uploadImage')}
              </Button>
              {hasImage && (
                <Button type="button" variant="tertiary" onClick={handleRemoveImage} disabled={submitting}>
                  {t('admin.associations.removeImage')}
                </Button>
              )}
            </div>
            {imageFile && (
              <p className="mt-2 text-body-md text-green">
                <i className="ri-checkbox-circle-line mr-1" aria-hidden="true"></i>
                {imageFile.name}
              </p>
            )}
            <input
              ref={imageInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp,image/gif"
              className="hidden"
              onChange={handleImageFileChange}
            />

            <div className="mt-4">
              <Field label={t('admin.associations.imageUrl')} htmlFor="imageUrl">
                <input
                  type="text"
                  inputMode="url"
                  id="imageUrl"
                  name="imageUrl"
                  value={formData.imageUrl}
                  onChange={(e) => handleImageUrlChange(e.target.value)}
                  className={inputClasses}
                  placeholder="https://"
                />
              </Field>
            </div>
          </div>
        }
      />
    </form>
  );
};
