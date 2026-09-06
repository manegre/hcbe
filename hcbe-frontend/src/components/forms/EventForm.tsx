import React, { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { ReactNode } from 'react';
import type { CreateEventRequest, UpdateEventRequest, Event } from '../../lib/api/types';
import type { CommunityOrganizer, EventCategory } from '../../lib/api/types';
import { useTranslation } from 'react-i18next';
import {
  AdminLanguageTabs,
  isEnglishContentIncomplete,
} from '../admin/AdminLanguageTabs';
import { AdminFormLayout } from '../admin/AdminFormLayout';
import { ArrowLink, Button, Field, RichTextEditor, inputClasses } from '../ui';
import { formatFileSize, resolveMediaUrl } from '../../lib/api/media-url';
import { eventCategoriesApi } from '../../lib/api/event-categories';
import { communityMarketplaceApi } from '../../lib/api/community-marketplace';
import {
  EVENT_TIME_ZONES,
  isoToZonedInput,
  zonedInputToIso,
} from '../../lib/events/timezone';

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
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const formRef = useRef<HTMLFormElement>(null);
  const attachmentInputRef = useRef<HTMLInputElement>(null);
  const coverInputRef = useRef<HTMLInputElement>(null);
  const initialTimeZone = initialValues?.timeZone || 'America/Toronto';
  const [formData, setFormData] = useState({
    title: initialValues?.title || '',
    titleEn: initialValues?.titleEn || '',
    description: initialValues?.description || '',
    descriptionEn: initialValues?.descriptionEn || '',
    date: isoToZonedInput(initialValues?.date, initialTimeZone),
    endDate: isoToZonedInput(initialValues?.endDate, initialTimeZone),
    timeZone: initialTimeZone,
    location: initialValues?.location || '',
    locationEn: initialValues?.locationEn || '',
    type: initialValues?.type || '',
    format: initialValues?.format || 'InPerson',
    zone: initialValues?.zone || '',
    capacity: initialValues?.capacity?.toString() || '',
    registrationDeadline: initialValues?.registrationDeadline
      ? new Date(initialValues.registrationDeadline).toISOString().slice(0, 16)
      : '',
    meetingLink: initialValues?.meetingLink || '',
    registrationUrl: initialValues?.registrationUrl || '',
    ctaLabel: initialValues?.ctaLabel || '',
    ctaLabelEn: initialValues?.ctaLabelEn || '',
    imageUrl: initialValues?.imageUrl || '',
    status: initialValues?.status || 'Draft',
    speakers: initialValues?.speakers?.length ? initialValues.speakers : [''],
    organizers: initialValues?.organizers?.length ? initialValues.organizers : [''],
    registrationMode: initialValues?.registrationMode || 'Native',
    allowWaitlist: initialValues?.allowWaitlist ?? true,
    restrictMeetingLinkToRegistrants: initialValues?.restrictMeetingLinkToRegistrants ?? true,
    ticketingEnabled: initialValues?.ticketingEnabled ?? false,
    salesModel: initialValues?.salesModel ?? 'HCBE',
    platformFeePercent: initialValues?.platformFeePercent?.toString() ?? '0',
    communityOrganizerId: initialValues?.communityOrganizerId ?? '',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [coverPreviewUrl, setCoverPreviewUrl] = useState('');
  const [categories, setCategories] = useState<EventCategory[]>([]);
  const [communityOrganizers, setCommunityOrganizers] = useState<CommunityOrganizer[]>([]);
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

  useEffect(() => {
    let active = true;
    eventCategoriesApi.getCategoriesForAdmin()
      .then((response) => {
        if (active && response.success && response.data) setCategories(response.data);
      })
      .catch(() => undefined);
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    communityMarketplaceApi.getAdminOrganizers().then((response) => {
      if (response.data) setCommunityOrganizers(response.data.filter((item) => item.status === 'Approved'));
    }).catch(() => undefined);
  }, []);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    const nextValue = e.target instanceof HTMLInputElement && e.target.type === 'checkbox'
      ? e.target.checked
      : value;
    setFormData((prev) => ({ ...prev, [name]: nextValue }));

    if (errors[name]) {
      setErrors((prev) => ({ ...prev, [name]: '' }));
    }
  };

  const updateSpeaker = (index: number, value: string) => {
    setFormData((prev) => ({
      ...prev,
      speakers: prev.speakers.map((speaker, speakerIndex) =>
        speakerIndex === index ? value : speaker,
      ),
    }));
    if (errors.speakers) {
      setErrors((prev) => ({ ...prev, speakers: '' }));
    }
  };

  const addSpeaker = () => {
    setFormData((prev) =>
      prev.speakers.length >= 20 ? prev : { ...prev, speakers: [...prev.speakers, ''] },
    );
  };

  const removeSpeaker = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      speakers:
        prev.speakers.length === 1
          ? ['']
          : prev.speakers.filter((_, speakerIndex) => speakerIndex !== index),
    }));
  };

  const updateOrganizer = (index: number, value: string) => {
    setFormData((prev) => ({
      ...prev,
      organizers: prev.organizers.map((organizer, organizerIndex) =>
        organizerIndex === index ? value : organizer,
      ),
    }));
    if (errors.organizers) setErrors((prev) => ({ ...prev, organizers: '' }));
  };

  const addOrganizer = () => {
    setFormData((prev) =>
      prev.organizers.length >= 20 ? prev : { ...prev, organizers: [...prev.organizers, ''] },
    );
  };

  const removeOrganizer = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      organizers:
        prev.organizers.length === 1
          ? ['']
          : prev.organizers.filter((_, organizerIndex) => organizerIndex !== index),
    }));
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.title.trim()) {
      newErrors.title = t('admin.events.form.validation.titleRequired');
    }

    if (!formData.date) {
      newErrors.date = t('admin.events.form.validation.dateRequired');
    }

    if (formData.endDate && formData.endDate <= formData.date) {
      newErrors.endDate = t('admin.events.form.validation.endAfterStart');
    }

    if (!formData.status) {
      newErrors.status = t('admin.events.form.validation.statusRequired');
    }

    if (
      formData.registrationDeadline &&
      formData.registrationDeadline >= formData.date
    ) {
      newErrors.registrationDeadline = t('admin.events.form.validation.deadlineBeforeDate');
    }

    if (
      formData.capacity &&
      (isNaN(parseInt(formData.capacity, 10)) || parseInt(formData.capacity, 10) < 1)
    ) {
      newErrors.capacity = t('admin.events.form.validation.capacityPositive');
    }

    if (formData.speakers.some((speaker) => speaker.trim().length > 160)) {
      newErrors.speakers = t('admin.events.form.validation.speakerNameTooLong');
    }

    if (formData.organizers.some((organizer) => organizer.trim().length > 160)) {
      newErrors.organizers = t('admin.events.form.validation.organizerNameTooLong');
    }

    if (formData.status === 'Active' && formData.format !== 'Online' && !formData.location.trim()) {
      newErrors.location = t('admin.events.form.validation.locationRequired');
    }

    if (formData.status === 'Active' && formData.format !== 'InPerson' && !formData.meetingLink.trim()) {
      newErrors.meetingLink = t('admin.events.form.validation.meetingLinkRequired');
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
      date: zonedInputToIso(formData.date, formData.timeZone),
      endDate: formData.endDate
        ? zonedInputToIso(formData.endDate, formData.timeZone)
        : undefined,
      timeZone: formData.timeZone,
      location: formData.location || undefined,
      locationEn: formData.locationEn,
      type: formData.type || undefined,
      format: formData.format as 'InPerson' | 'Online' | 'Hybrid',
      zone: formData.zone || undefined,
      capacity: formData.capacity ? parseInt(formData.capacity, 10) : undefined,
      registrationDeadline: formData.registrationDeadline
        ? zonedInputToIso(formData.registrationDeadline, formData.timeZone)
        : undefined,
      meetingLink: formData.meetingLink || undefined,
      registrationUrl: formData.registrationUrl || undefined,
      ctaLabel: formData.ctaLabel || undefined,
      ctaLabelEn: formData.ctaLabelEn || undefined,
      imageUrl: formData.imageUrl || undefined,
      status: formData.status,
      speakers: formData.speakers.map((speaker) => speaker.trim()).filter(Boolean),
      organizers: formData.organizers.map((organizer) => organizer.trim()).filter(Boolean),
      registrationMode: formData.registrationMode as 'Disabled' | 'External' | 'Native',
      allowWaitlist: formData.allowWaitlist,
      restrictMeetingLinkToRegistrants: formData.restrictMeetingLinkToRegistrants,
      ticketingEnabled: formData.ticketingEnabled,
      salesModel: formData.salesModel as 'HCBE' | 'Community',
      platformFeePercent: Number(formData.platformFeePercent || 0),
      communityOrganizerId: formData.communityOrganizerId || undefined,
      clearCommunityOrganizer: formData.salesModel === 'HCBE',
    };

    await onSubmit(submitData);
  };

  const statusOptions = [
    { value: 'Draft', label: t('admin.eventPublication.draft') },
    { value: 'Active', label: t('admin.events.form.statusPublished') },
    { value: 'Cancelled', label: t('admin.eventPublication.cancelled') },
    { value: 'Completed', label: t('admin.eventPublication.completed') },
  ];

  const fallbackTypeOptions = [
    { slug: 'workshop', name: t('admin.events.type.workshop'), nameEn: 'Workshop' },
    { slug: 'conference', name: t('admin.events.type.conference'), nameEn: 'Conference' },
    { slug: 'webinar', name: t('admin.events.type.webinar'), nameEn: 'Webinar' },
    { slug: 'professional-development', name: t('admin.events.type.professionalDevelopment'), nameEn: 'Professional development' },
    { slug: 'diplomatic-community-meeting', name: t('admin.events.type.diplomaticMeeting'), nameEn: 'Diplomatic and community meeting' },
    { slug: 'business-investment', name: t('admin.events.type.businessInvestment'), nameEn: 'Business and investment' },
    { slug: 'networking', name: t('admin.events.type.networking'), nameEn: 'Networking' },
    { slug: 'training', name: t('admin.events.type.training'), nameEn: 'Training' },
    { slug: 'cultural-festival', name: t('admin.events.type.culturalFestival'), nameEn: 'Cultural festival' },
    { slug: 'national-celebration', name: t('admin.events.type.nationalCelebration'), nameEn: 'National and civic celebration' },
    { slug: 'fundraiser-solidarity', name: t('admin.events.type.fundraiser'), nameEn: 'Fundraiser and solidarity' },
    { slug: 'memorial-tribute', name: t('admin.events.type.memorial'), nameEn: 'Memorial and tribute' },
    { slug: 'social', name: t('admin.events.type.social'), nameEn: 'Social event' },
    { slug: 'other', name: t('admin.events.type.other'), nameEn: 'Other' },
  ];
  const categoryOptions = categories.length > 0
    ? categories.filter((category) => category.isActive || category.slug === formData.type)
    : fallbackTypeOptions;

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
          <div className="flex flex-wrap items-center gap-4">
            <ArrowLink to="/admin/events/categories" tone="green">
              {t('admin.events.categories.manage')}
            </ArrowLink>
            {initialValues && (
              <ArrowLink to={`/admin/events/${initialValues.id}`} tone="green">
                {t('admin.events.view')}
              </ArrowLink>
            )}
          </div>
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
              [formData.ctaLabel, formData.ctaLabelEn],
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
                    <RichTextEditor id="description" value={formData.description} onChange={(description) => setFormData((current) => ({ ...current, description }))} label={t('admin.common.description')} placeholder={t('admin.events.form.descriptionPlaceholder')} minHeight={260} />
                  </Field>
                </div>

                <div className="md:col-span-2">
                  <Field label={t('admin.common.location')} htmlFor="location" error={errors.location}>
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

                <div className="md:col-span-2">
                  <Field
                    label={t('admin.events.form.ctaLabel')}
                    htmlFor="ctaLabel"
                    hint={t('admin.events.form.ctaLabelHint')}
                  >
                    <input
                      type="text"
                      id="ctaLabel"
                      name="ctaLabel"
                      value={formData.ctaLabel}
                      onChange={handleChange}
                      className={inputClasses}
                      placeholder={t('admin.events.form.ctaLabelPlaceholder')}
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
                  <Field label={t('admin.events.form.ctaLabel')} htmlFor="ctaLabelEn">
                    <input
                      type="text"
                      id="ctaLabelEn"
                      name="ctaLabelEn"
                      value={formData.ctaLabelEn}
                      onChange={handleChange}
                      className={inputClasses}
                      placeholder={t('admin.events.form.ctaLabelEnPlaceholder')}
                    />
                  </Field>
                </div>

                <div className="md:col-span-2">
                  <Field label={t('admin.common.description')} htmlFor="descriptionEn">
                    <RichTextEditor id="descriptionEn" value={formData.descriptionEn || ''} onChange={(descriptionEn) => setFormData((current) => ({ ...current, descriptionEn }))} label={t('admin.common.description')} placeholder={t('admin.events.form.descriptionEnPlaceholder')} minHeight={260} />
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
            <section aria-labelledby="event-schedule-title">
              <div className="mb-5 flex items-start gap-3 border-b border-line pb-4">
                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-green text-gold">
                  <i className="ri-calendar-schedule-line text-lg" aria-hidden="true" />
                </span>
                <div>
                  <h2 id="event-schedule-title" className="font-display text-title-lg text-green">
                    {t('admin.events.form.scheduleSection')}
                  </h2>
                  <p className="mt-1 text-body-md text-ink-variant">
                    {t('admin.events.form.scheduleHint')}
                  </p>
                </div>
              </div>

              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
              <Field label={t('admin.events.form.startDate')} htmlFor="date" required error={errors.date}>
                <input
                  type="datetime-local"
                  id="date"
                  name="date"
                  value={formData.date}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>

              <Field label={t('admin.events.form.endDate')} htmlFor="endDate" error={errors.endDate}>
                <input
                  type="datetime-local"
                  id="endDate"
                  name="endDate"
                  value={formData.endDate}
                  onChange={handleChange}
                  min={formData.date || undefined}
                  className={inputClasses}
                />
              </Field>

              <Field
                label={t('admin.events.form.timeZone')}
                htmlFor="timeZone"
                hint={t('admin.events.form.timeZoneHint')}
              >
                <select
                  id="timeZone"
                  name="timeZone"
                  value={formData.timeZone}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  {EVENT_TIME_ZONES.map((timeZone) => (
                    <option key={timeZone.value} value={timeZone.value}>{timeZone.label}</option>
                  ))}
                </select>
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
              </div>
            </section>

            <section className="mt-8 border-t border-line pt-6" aria-labelledby="event-format-title">
              <div className="mb-5 flex items-start gap-3">
                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-gold text-green">
                  <i className="ri-map-pin-2-line text-lg" aria-hidden="true" />
                </span>
                <div>
                  <h2 id="event-format-title" className="font-display text-title-lg text-green">
                    {t('admin.events.form.formatSection')}
                  </h2>
                  <p className="mt-1 text-body-md text-ink-variant">{t('admin.events.form.formatHint')}</p>
                </div>
              </div>

              <div className="mb-6 grid grid-cols-3 gap-2">
                {(['InPerson', 'Online', 'Hybrid'] as const).map((format) => (
                  <button
                    key={format}
                    type="button"
                    onClick={() => setFormData((current) => ({ ...current, format }))}
                    className={`min-h-[72px] rounded-xl border px-3 py-3 text-center transition-all ${
                      formData.format === format
                        ? 'border-green bg-green text-white shadow-[0_8px_20px_rgba(0,59,27,.14)]'
                        : 'border-line bg-surface text-ink hover:border-green/40'
                    }`}
                    aria-pressed={formData.format === format}
                  >
                    <i
                      className={`${format === 'InPerson' ? 'ri-map-pin-line' : format === 'Online' ? 'ri-live-line' : 'ri-links-line'} block text-xl ${formData.format === format ? 'text-gold' : 'text-green'}`}
                      aria-hidden="true"
                    />
                    <span className="mt-1 block text-[10px] font-bold uppercase tracking-[.1em]">
                      {t(`admin.events.format.${format}`)}
                    </span>
                  </button>
                ))}
              </div>

              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">

              <Field
                label={t('admin.events.form.meetingLink')}
                htmlFor="meetingLink"
                error={errors.meetingLink}
                hint={formData.format === 'InPerson' ? t('admin.events.form.meetingLinkOptional') : undefined}
              >
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
                  <option value="">{t('admin.events.form.selectType')}</option>
                  {categoryOptions.map((option) => (
                    <option key={option.slug} value={option.slug}>
                      {i18n.language.startsWith('en') ? option.nameEn || option.name : option.name}
                    </option>
                  ))}
                  {formData.type && !categoryOptions.some((option) => option.slug === formData.type) && (
                    <option value={formData.type}>{formData.type}</option>
                  )}
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

              <Field label={i18n.language.startsWith('fr') ? "Mode d'inscription" : 'Registration mode'} htmlFor="registrationMode">
                <select
                  id="registrationMode"
                  name="registrationMode"
                  value={formData.registrationMode}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="Native">{i18n.language.startsWith('fr') ? 'Inscription HCBE intégrée' : 'Built-in HCBE registration'}</option>
                  <option value="External">{i18n.language.startsWith('fr') ? 'Lien externe' : 'External link'}</option>
                  <option value="Disabled">{i18n.language.startsWith('fr') ? 'Aucune inscription' : 'No registration'}</option>
                </select>
              </Field>

              {formData.registrationMode === 'External' && <Field label={t('admin.events.form.registrationUrl')} htmlFor="registrationUrl">
                <input
                  type="url"
                  id="registrationUrl"
                  name="registrationUrl"
                  value={formData.registrationUrl}
                  onChange={handleChange}
                  className={inputClasses}
                  placeholder="https://..."
                />
              </Field>}

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

              <div className="mt-6 rounded-2xl border border-gold/35 bg-gold/[.07] p-5">
                <label className="flex cursor-pointer items-start gap-4">
                  <input type="checkbox" name="ticketingEnabled" checked={formData.ticketingEnabled} onChange={handleChange} className="mt-1 h-5 w-5 accent-green" />
                  <span><strong className="block font-display text-xl text-green">{i18n.language.startsWith('fr') ? 'Activer la billetterie HCBE' : 'Enable HCBE ticketing'}</strong><small className="mt-1 block text-sm leading-5 text-ink-variant">{i18n.language.startsWith('fr') ? 'Vendez des billets gratuits ou payants, générez les QR et suivez les entrées.' : 'Sell free or paid tickets, generate QR codes, and track admission.'}</small></span>
                </label>
                {formData.ticketingEnabled && <div className="mt-5 grid gap-4 border-t border-gold/25 pt-5 sm:grid-cols-2"><Field label={i18n.language.startsWith('fr') ? 'Compte vendeur' : 'Seller account'} htmlFor="salesModel"><select id="salesModel" name="salesModel" value={formData.salesModel} onChange={handleChange} className={`${inputClasses} cursor-pointer`}><option value="HCBE">HCBE Canada</option><option value="Community">{i18n.language.startsWith('fr') ? 'Organisateur communautaire approuvé' : 'Approved community organizer'}</option></select></Field><Field label={i18n.language.startsWith('fr') ? 'Frais de plateforme (%)' : 'Platform fee (%)'} htmlFor="platformFeePercent"><input id="platformFeePercent" name="platformFeePercent" type="number" min="0" max="25" value={formData.platformFeePercent} onChange={handleChange} className={inputClasses} /></Field>{formData.salesModel === 'Community' && <div className="sm:col-span-2"><Field label={i18n.language.startsWith('fr') ? 'Organisateur bénéficiaire' : 'Beneficiary organizer'} htmlFor="communityOrganizerId"><select id="communityOrganizerId" name="communityOrganizerId" required value={formData.communityOrganizerId} onChange={handleChange} className={`${inputClasses} cursor-pointer`}><option value="">{i18n.language.startsWith('fr') ? 'Sélectionnez un organisateur approuvé' : 'Select an approved organizer'}</option>{communityOrganizers.map((item) => <option key={item.id} value={item.id}>{item.displayName} · {item.contactEmail}</option>)}</select></Field></div>}<p className="sm:col-span-2 text-xs text-ink-variant">{i18n.language.startsWith('fr') ? "Après avoir enregistré l’événement, ajoutez les tarifs depuis sa fiche détaillée." : 'After saving the event, add ticket tiers from its detail page.'}</p></div>}
              </div>

              {formData.registrationMode === 'Native' && !formData.ticketingEnabled && (
                <div className="mt-6 grid gap-3 sm:grid-cols-2">
                  <label className="flex min-h-16 cursor-pointer items-center gap-3 rounded-xl border border-line bg-surface px-4 py-3 text-sm text-ink">
                    <input type="checkbox" name="allowWaitlist" checked={formData.allowWaitlist} onChange={handleChange} className="h-4 w-4 accent-green" />
                    <span>{i18n.language.startsWith('fr') ? "Activer la liste d'attente" : 'Enable waiting list'}</span>
                  </label>
                  <label className="flex min-h-16 cursor-pointer items-center gap-3 rounded-xl border border-line bg-surface px-4 py-3 text-sm text-ink">
                    <input type="checkbox" name="restrictMeetingLinkToRegistrants" checked={formData.restrictMeetingLinkToRegistrants} onChange={handleChange} className="h-4 w-4 accent-green" />
                    <span>{i18n.language.startsWith('fr') ? 'Réserver le lien aux inscrits' : 'Restrict meeting link to registrants'}</span>
                  </label>
                </div>
              )}
            </section>

            <section className="mt-8 border-t border-line pt-6" aria-labelledby="event-speakers-title">
              <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
                <div>
                  <h3
                    id="event-speakers-title"
                    className="flex items-center gap-2 text-label-md uppercase text-green"
                  >
                    <i className="ri-user-voice-line text-gold-ink" aria-hidden="true" />
                    {t('admin.events.form.speakers')}
                  </h3>
                  <p className="mt-1 text-body-md text-ink-variant">
                    {t('admin.events.form.speakersHint')}
                  </p>
                </div>
                <Button
                  type="button"
                  variant="secondary"
                  onClick={addSpeaker}
                  disabled={isLoading || formData.speakers.length >= 20}
                >
                  <i className="ri-add-line" aria-hidden="true" />
                  {t('admin.events.form.addSpeaker')}
                </Button>
              </div>

              <div className="mt-5 space-y-3">
                {formData.speakers.map((speaker, index) => (
                  <div
                    key={index}
                    className="grid grid-cols-[2.5rem_minmax(0,1fr)_2.75rem] items-end gap-3"
                  >
                    <span className="flex h-11 items-center justify-center border border-line bg-surface-container font-display text-title-md text-green">
                      {String(index + 1).padStart(2, '0')}
                    </span>
                    <Field
                      label={t('admin.events.form.speakerNumber', { number: index + 1 })}
                      htmlFor={`speaker-${index}`}
                    >
                      <input
                        type="text"
                        id={`speaker-${index}`}
                        value={speaker}
                        onChange={(event) => updateSpeaker(index, event.target.value)}
                        maxLength={160}
                        className={inputClasses}
                        placeholder={t('admin.events.form.speakerPlaceholder')}
                      />
                    </Field>
                    <button
                      type="button"
                      onClick={() => removeSpeaker(index)}
                      disabled={isLoading}
                      className="flex h-11 w-11 items-center justify-center border border-line text-ink-variant transition-colors hover:border-error hover:text-error focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-green disabled:cursor-not-allowed disabled:opacity-50"
                      aria-label={t('admin.events.form.removeSpeaker', { number: index + 1 })}
                    >
                      <i className="ri-delete-bin-line" aria-hidden="true" />
                    </button>
                  </div>
                ))}
              </div>

              {errors.speakers && (
                <p className="mt-3 text-body-md text-error" role="alert">
                  {errors.speakers}
                </p>
              )}
            </section>

            <section className="mt-8 border-t border-line pt-6" aria-labelledby="event-organizers-title">
              <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
                <div>
                  <h3
                    id="event-organizers-title"
                    className="flex items-center gap-2 text-label-md uppercase text-green"
                  >
                    <i className="ri-community-line text-gold-ink" aria-hidden="true" />
                    {t('admin.events.form.organizers')}
                  </h3>
                  <p className="mt-1 text-body-md text-ink-variant">
                    {t('admin.events.form.organizersHint')}
                  </p>
                </div>
                <Button
                  type="button"
                  variant="secondary"
                  onClick={addOrganizer}
                  disabled={isLoading || formData.organizers.length >= 20}
                >
                  <i className="ri-add-line" aria-hidden="true" />
                  {t('admin.events.form.addOrganizer')}
                </Button>
              </div>

              <div className="mt-5 space-y-3">
                {formData.organizers.map((organizer, index) => (
                  <div
                    key={index}
                    className="grid grid-cols-[2.5rem_minmax(0,1fr)_2.75rem] items-end gap-3"
                  >
                    <span className="flex h-11 items-center justify-center rounded-lg border border-line bg-surface-container font-display text-title-md text-green">
                      {String(index + 1).padStart(2, '0')}
                    </span>
                    <Field
                      label={t('admin.events.form.organizerNumber', { number: index + 1 })}
                      htmlFor={`organizer-${index}`}
                    >
                      <input
                        type="text"
                        id={`organizer-${index}`}
                        value={organizer}
                        onChange={(event) => updateOrganizer(index, event.target.value)}
                        maxLength={160}
                        className={inputClasses}
                        placeholder={t('admin.events.form.organizerPlaceholder')}
                      />
                    </Field>
                    <button
                      type="button"
                      onClick={() => removeOrganizer(index)}
                      disabled={isLoading}
                      className="flex h-11 w-11 items-center justify-center rounded-lg border border-line text-ink-variant transition-colors hover:border-error hover:text-error focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-green disabled:cursor-not-allowed disabled:opacity-50"
                      aria-label={t('admin.events.form.removeOrganizer', { number: index + 1 })}
                    >
                      <i className="ri-delete-bin-line" aria-hidden="true" />
                    </button>
                  </div>
                ))}
              </div>

              {errors.organizers && (
                <p className="mt-3 text-body-md text-error" role="alert">{errors.organizers}</p>
              )}
            </section>
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
