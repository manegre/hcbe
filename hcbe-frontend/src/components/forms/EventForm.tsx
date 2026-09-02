import React, { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { ReactNode } from 'react';
import type { CreateEventRequest, UpdateEventRequest, Event } from '../../lib/api/types';
import { useTranslation } from 'react-i18next';
import {
  AdminLanguageTabs,
  isEnglishContentIncomplete,
} from '../admin/AdminLanguageTabs';
import { AdminFormLayout } from '../admin/AdminFormLayout';
import { ArrowLink, Button, Field, inputClasses } from '../ui';
import { formatFileSize, resolveMediaUrl } from '../../lib/api/media-url';

interface EventFormProps {
  initialValues?: Event;
  onSubmit: (data: CreateEventRequest | UpdateEventRequest) => Promise<void>;
  isLoading: boolean;
  submitButtonText?: string;
  pendingAttachments?: File[];
  onPendingAttachmentsChange?: (files: File[]) => void;
  coverFile?: File | null;
  onCoverFileChange?: (file: File | null) => void;
  /** Rendered in the aside column, below the cover image panel — used by the
   * edit page to slot in EventGalleryManager / EventAttachmentsManager. */
  aside?: ReactNode;
}

export const EventForm: React.FC<EventFormProps> = ({
  initialValues,
  onSubmit,
  isLoading,
  submitButtonText,
  pendingAttachments = [],
  onPendingAttachmentsChange,
  coverFile = null,
  onCoverFileChange,
  aside,
}) => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const formRef = useRef<HTMLFormElement>(null);
  const attachmentInputRef = useRef<HTMLInputElement>(null);
  const coverInputRef = useRef<HTMLInputElement>(null);
  const [formData, setFormData] = useState({
    title: initialValues?.title || '',
    titleEn: initialValues?.titleEn || '',
    description: initialValues?.description || '',
    descriptionEn: initialValues?.descriptionEn || '',
    date: initialValues?.date ? new Date(initialValues.date).toISOString().slice(0, 16) : '',
    location: initialValues?.location || '',
    locationEn: initialValues?.locationEn || '',
    type: initialValues?.type || '',
    zone: initialValues?.zone || '',
    capacity: initialValues?.capacity?.toString() || '',
    registrationDeadline: initialValues?.registrationDeadline
      ? new Date(initialValues.registrationDeadline).toISOString().slice(0, 16)
      : '',
    meetingLink: initialValues?.meetingLink || '',
    imageUrl: initialValues?.imageUrl || '',
    status: initialValues?.status || 'Draft',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [coverPreviewUrl, setCoverPreviewUrl] = useState('');
  const initialSnapshotRef = useRef(JSON.stringify(formData));
  const isDirty = JSON.stringify(formData) !== initialSnapshotRef.current;

  const hasCover = Boolean(coverFile || formData.imageUrl);
  const backPath = '/admin/events';
  const backLabel = t('admin.events.backToList');
  const title = initialValues ? t('admin.events.editTitle') : t('admin.events.createTitle');

  useEffect(() => {
    if (!coverFile) {
      setCoverPreviewUrl(resolveMediaUrl(formData.imageUrl));
      return;
    }
    const objectUrl = URL.createObjectURL(coverFile);
    setCoverPreviewUrl(objectUrl);
    return () => URL.revokeObjectURL(objectUrl);
  }, [coverFile, formData.imageUrl]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));

    if (errors[name]) {
      setErrors((prev) => ({ ...prev, [name]: '' }));
    }
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.title.trim()) {
      newErrors.title = t('admin.events.form.validation.titleRequired');
    }

    if (!formData.date) {
      newErrors.date = t('admin.events.form.validation.dateRequired');
    }

    if (!formData.status) {
      newErrors.status = t('admin.events.form.validation.statusRequired');
    }

    if (
      formData.registrationDeadline &&
      new Date(formData.registrationDeadline) >= new Date(formData.date)
    ) {
      newErrors.registrationDeadline = t('admin.events.form.validation.deadlineBeforeDate');
    }

    if (
      formData.capacity &&
      (isNaN(parseInt(formData.capacity, 10)) || parseInt(formData.capacity, 10) < 1)
    ) {
      newErrors.capacity = t('admin.events.form.validation.capacityPositive');
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    const submitData: CreateEventRequest | UpdateEventRequest = {
      title: formData.title,
      titleEn: formData.titleEn,
      description: formData.description || undefined,
      descriptionEn: formData.descriptionEn,
      date: new Date(formData.date).toISOString(),
      location: formData.location || undefined,
      locationEn: formData.locationEn,
      type: formData.type || undefined,
      zone: formData.zone || undefined,
      capacity: formData.capacity ? parseInt(formData.capacity, 10) : undefined,
      registrationDeadline: formData.registrationDeadline
        ? new Date(formData.registrationDeadline).toISOString()
        : undefined,
      meetingLink: formData.meetingLink || undefined,
      imageUrl: formData.imageUrl || undefined,
      status: formData.status,
    };

    await onSubmit(submitData);
  };

  const statusOptions = [
    { value: 'Draft', label: t('admin.eventPublication.draft') },
    { value: 'Active', label: t('admin.events.form.statusPublished') },
    { value: 'Cancelled', label: t('admin.eventPublication.cancelled') },
    { value: 'Completed', label: t('admin.eventPublication.completed') },
  ];

  const typeOptions = [
    { value: '', label: t('admin.events.form.selectType') },
    { value: 'Workshop', label: t('admin.events.type.workshop') },
    { value: 'Conference', label: t('admin.events.type.conference') },
    { value: 'Networking', label: t('admin.events.type.networking') },
    { value: 'Training', label: t('admin.events.type.training') },
    { value: 'Social', label: t('admin.events.type.social') },
    { value: 'Other', label: t('admin.events.type.other') },
  ];

  const zoneOptions = [
    { value: '', label: t('admin.events.form.selectZone') },
    { value: 'Montreal', label: t('admin.events.zone.montreal') },
    { value: 'Quebec', label: t('admin.events.zone.quebec') },
    { value: 'Ottawa', label: t('admin.events.zone.ottawa') },
    { value: 'Toronto', label: t('admin.events.zone.toronto') },
    { value: 'Virtual', label: t('admin.events.zone.virtual') },
    { value: 'Other', label: t('admin.events.zone.other') },
  ];

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title={title}
        backPath={backPath}
        backLabel={backLabel}
        isDirty={isDirty}
        dirtyLabel={t('admin.common.unsavedChanges')}
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        secondaryActions={
          initialValues && (
            <ArrowLink to={`/admin/events/${initialValues.id}`} tone="green">
              {t('admin.events.view')}
            </ArrowLink>
          )
        }
        actions={
          <Button type="submit" variant="primary" disabled={isLoading}>
            {isLoading ? t('admin.events.form.saving') : (submitButtonText ?? t('admin.common.save'))}
          </Button>
        }
        languageTabs={
          <AdminLanguageTabs
            enIncomplete={isEnglishContentIncomplete([
              [formData.title, formData.titleEn],
              [formData.description, formData.descriptionEn],
              [formData.location, formData.locationEn],
            ])}
            frPanel={
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <div className="md:col-span-2">
                  <Field label={t('admin.events.form.title')} htmlFor="title" required error={errors.title}>
                    <input
                      type="text"
                      id="title"
                      name="title"
                      value={formData.title}
                      onChange={handleChange}
                      className={inputClasses}
                      placeholder={t('admin.events.form.titlePlaceholder')}
                    />
                  </Field>
                </div>

                <div className="md:col-span-2">
                  <Field label={t('admin.common.description')} htmlFor="description">
                    <textarea
                      id="description"
                      name="description"
                      rows={4}
                      value={formData.description}
                      onChange={handleChange}
                      className={inputClasses}
                      placeholder={t('admin.events.form.descriptionPlaceholder')}
                    />
                  </Field>
                </div>

                <div className="md:col-span-2">
                  <Field label={t('admin.common.location')} htmlFor="location">
                    <input
                      type="text"
                      id="location"
                      name="location"
                      value={formData.location}
                      onChange={handleChange}
                      className={inputClasses}
                      placeholder={t('admin.events.form.locationPlaceholder')}
                    />
                  </Field>
                </div>
              </div>
            }
            enPanel={
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <div className="md:col-span-2">
                  <Field label={t('admin.events.form.title')} htmlFor="titleEn">
                    <input
                      type="text"
                      id="titleEn"
                      name="titleEn"
                      value={formData.titleEn}
                      onChange={handleChange}
                      className={inputClasses}
                      placeholder={t('admin.events.form.titleEnPlaceholder')}
                    />
                  </Field>
                </div>

                <div className="md:col-span-2">
                  <Field label={t('admin.common.description')} htmlFor="descriptionEn">
                    <textarea
                      id="descriptionEn"
                      name="descriptionEn"
                      rows={4}
                      value={formData.descriptionEn}
                      onChange={handleChange}
                      className={inputClasses}
                      placeholder={t('admin.events.form.descriptionEnPlaceholder')}
                    />
                  </Field>
                </div>

                <div className="md:col-span-2">
                  <Field label={t('admin.common.location')} htmlFor="locationEn">
                    <input
                      type="text"
                      id="locationEn"
                      name="locationEn"
                      value={formData.locationEn}
                      onChange={handleChange}
                      className={inputClasses}
                      placeholder={t('admin.events.form.locationEnPlaceholder')}
                    />
                  </Field>
                </div>
              </div>
            }
          />
        }
        main={
          <div>
            <h2 className="mb-4 border-b border-line pb-3 text-label-md uppercase text-ink-variant">
              {t('admin.content.lang.settings')}
            </h2>
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
              <Field label={t('admin.events.form.eventDate')} htmlFor="date" required error={errors.date}>
                <input
                  type="datetime-local"
                  id="date"
                  name="date"
                  value={formData.date}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>

              <Field
                label={t('admin.events.registrationDeadline')}
                htmlFor="registrationDeadline"
                error={errors.registrationDeadline}
              >
                <input
                  type="datetime-local"
                  id="registrationDeadline"
                  name="registrationDeadline"
                  value={formData.registrationDeadline}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>

              <Field label={t('admin.events.form.meetingLink')} htmlFor="meetingLink">
                <input
                  type="url"
                  id="meetingLink"
                  name="meetingLink"
                  value={formData.meetingLink}
                  onChange={handleChange}
                  className={inputClasses}
                  placeholder="https://zoom.us/j/..."
                />
              </Field>

              <Field label={t('admin.events.form.eventType')} htmlFor="type">
                <select
                  id="type"
                  name="type"
                  value={formData.type}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  {typeOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label={t('admin.common.zone')} htmlFor="zone">
                <select
                  id="zone"
                  name="zone"
                  value={formData.zone}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  {zoneOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label={t('admin.common.capacity')} htmlFor="capacity" error={errors.capacity}>
                <input
                  type="number"
                  id="capacity"
                  name="capacity"
                  value={formData.capacity}
                  onChange={handleChange}
                  min="1"
                  className={inputClasses}
                  placeholder={t('admin.events.form.capacityPlaceholder')}
                />
              </Field>

              <Field
                label={t('admin.common.status')}
                htmlFor="status"
                required
                hint={t('admin.events.form.statusHint')}
                error={errors.status}
              >
                <select
                  id="status"
                  name="status"
                  value={formData.status}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  {statusOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </Field>
            </div>
          </div>
        }
        aside={
          <>
            <div className="border border-line bg-surface p-6">
              <h2 className="mb-1 text-label-md uppercase text-ink-variant">
                {t('admin.events.form.coverImage')}
              </h2>
              <p className="mb-4 text-body-md text-ink-variant">{t('admin.events.form.coverImageHint')}</p>

              {coverPreviewUrl ? (
                <img src={coverPreviewUrl} alt="" className="mb-4 aspect-square w-full border border-line object-cover" />
              ) : (
                <div className="mb-4 flex aspect-square w-full items-center justify-center border border-line bg-surface-container text-ink-variant">
                  <i className="ri-image-add-line text-4xl" aria-hidden="true" />
                </div>
              )}

              <div className="flex flex-wrap items-center gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => coverInputRef.current?.click()}
                  disabled={isLoading || !onCoverFileChange}
                >
                  {t('admin.events.form.uploadCover')}
                </Button>
                {hasCover && onCoverFileChange && (
                  <Button
                    type="button"
                    variant="tertiary"
                    disabled={isLoading}
                    onClick={() => {
                      onCoverFileChange(null);
                      setFormData((prev) => ({ ...prev, imageUrl: '' }));
                      if (coverInputRef.current) coverInputRef.current.value = '';
                    }}
                  >
                    {t('admin.events.form.removeCover')}
                  </Button>
                )}
              </div>
              {coverFile && (
                <p className="mt-2 text-body-md text-green">
                  <i className="ri-checkbox-circle-line mr-1" aria-hidden="true" />
                  {coverFile.name}
                </p>
              )}
              <input
                ref={coverInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp,image/gif"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0] ?? null;
                  onCoverFileChange?.(file);
                  if (file) {
                    setFormData((prev) => ({ ...prev, imageUrl: '' }));
                  }
                }}
              />

              <div className="mt-4">
                <Field label={t('admin.events.form.imageUrl')} htmlFor="imageUrl" hint={t('admin.events.form.imageUrlHint')}>
                  <input
                    type="text"
                    inputMode="url"
                    id="imageUrl"
                    name="imageUrl"
                    value={formData.imageUrl}
                    onChange={(e) => {
                      const value = e.target.value;
                      setFormData((prev) => ({ ...prev, imageUrl: value }));
                      if (value && onCoverFileChange) {
                        onCoverFileChange(null);
                        if (coverInputRef.current) coverInputRef.current.value = '';
                      }
                    }}
                    className={inputClasses}
                    placeholder="https://"
                  />
                </Field>
              </div>
            </div>

            {onPendingAttachmentsChange && (
              <div className="border border-line bg-surface p-6">
                <h2 className="mb-1 text-label-md uppercase text-ink-variant">
                  {t('admin.events.attachments.title')}
                </h2>
                <p className="mb-4 text-body-md text-ink-variant">{t('admin.events.attachments.createHint')}</p>

                {pendingAttachments.length > 0 && (
                  <ul className="mb-3 space-y-2">
                    {pendingAttachments.map((file, index) => (
                      <li
                        key={`${file.name}-${index}`}
                        className="flex items-center justify-between gap-3 border border-line px-3 py-2"
                      >
                        <span className="min-w-0 flex-1 truncate text-body-md text-ink">
                          <i className="ri-upload-2-line mr-2 text-green" aria-hidden="true" />
                          {file.name}
                          <span className="ml-2 text-ink-variant">({formatFileSize(file.size)})</span>
                        </span>
                        <button
                          type="button"
                          onClick={() =>
                            onPendingAttachmentsChange(pendingAttachments.filter((_, i) => i !== index))
                          }
                          disabled={isLoading}
                          className="text-body-md text-red-link transition-colors hover:text-green"
                        >
                          {t('admin.common.delete')}
                        </button>
                      </li>
                    ))}
                  </ul>
                )}

                <input
                  ref={attachmentInputRef}
                  type="file"
                  accept=".pdf,.doc,.docx,.xls,.xlsx,image/jpeg,image/png,image/webp,image/gif"
                  multiple
                  className="hidden"
                  onChange={(e) => {
                    const files = e.target.files ? Array.from(e.target.files) : [];
                    if (files.length > 0) {
                      onPendingAttachmentsChange([...pendingAttachments, ...files]);
                    }
                    e.target.value = '';
                  }}
                />
                <Button type="button" variant="secondary" disabled={isLoading} onClick={() => attachmentInputRef.current?.click()}>
                  <i className="ri-attachment-2" aria-hidden="true" />
                  {t('admin.events.attachments.add')}
                </Button>
                <p className="mt-3 text-body-md text-ink-variant">{t('admin.events.attachments.afterCreateNote')}</p>
              </div>
            )}

            {aside}
          </>
        }
      />
    </form>
  );
};
