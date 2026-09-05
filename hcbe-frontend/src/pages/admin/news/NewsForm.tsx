import React, { useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AdminLanguageTabs,
  isEnglishContentIncomplete,
} from '../../../components/admin/AdminLanguageTabs';
import { AdminFormLayout } from '../../../components/admin/AdminFormLayout';
import { Button, Field, RichTextEditor, inputClasses } from '../../../components/ui';
import { NEWS_CATEGORIES, getNewsCategoryLabelKey } from '../../../lib/news/category-styles';
import {
  NEWS_IMAGE_POSITIONS,
  newsImageObjectPositionClass,
  resolveNewsImagePosition,
  type NewsImagePosition,
} from '../../../lib/news/image-position';
import { formatFileSize, resolveMediaUrl } from '../../../lib/api/media-url';
import type { CreateNewsRequest, NewsAttachment } from '../../../lib/api/types';

interface NewsFormProps {
  formData: CreateNewsRequest;
  onChange: (data: CreateNewsRequest) => void;
  onSubmit: (e: React.FormEvent) => void;
  submitting: boolean;
  submitLabel: string;
  onCancel: () => void;
  coverFile: File | null;
  onCoverFileChange: (file: File | null) => void;
  pendingAttachments: File[];
  onPendingAttachmentsChange: (files: File[]) => void;
  existingAttachments?: NewsAttachment[];
  onDeleteAttachment?: (attachmentId: string) => Promise<void> | void;
}

export const NewsForm: React.FC<NewsFormProps> = ({
  formData,
  onChange,
  onSubmit,
  submitting,
  submitLabel,
  onCancel,
  coverFile,
  onCoverFileChange,
  pendingAttachments,
  onPendingAttachmentsChange,
  existingAttachments = [],
  onDeleteAttachment,
}) => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const formRef = useRef<HTMLFormElement>(null);
  const coverInputRef = useRef<HTMLInputElement>(null);
  const attachmentInputRef = useRef<HTMLInputElement>(null);
  const [deletingAttachmentId, setDeletingAttachmentId] = useState<string | null>(null);
  const [coverPreviewUrl, setCoverPreviewUrl] = useState('');
  const imagePosition = resolveNewsImagePosition(formData.imagePosition);
  const hasCover = Boolean(coverFile || formData.imageUrl);

  const initialSnapshotRef = useRef(JSON.stringify(formData));
  const isDirty = JSON.stringify(formData) !== initialSnapshotRef.current;

  const backPath = id ? `/admin/news/${id}` : '/admin/news';
  const backLabel = id ? t('admin.common.back') : t('admin.common.backToList');
  const title = id ? t('admin.news.editTitle') : t('admin.news.createTitle');

  useEffect(() => {
    if (!coverFile) {
      setCoverPreviewUrl(resolveMediaUrl(formData.imageUrl));
      return;
    }

    const objectUrl = URL.createObjectURL(coverFile);
    setCoverPreviewUrl(objectUrl);
    return () => URL.revokeObjectURL(objectUrl);
  }, [coverFile, formData.imageUrl]);

  const updateField = (field: keyof CreateNewsRequest, value: string | boolean) => {
    onChange({ ...formData, [field]: value });
  };

  const handleCoverChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    onCoverFileChange(file);
    // File upload takes precedence over a pasted URL.
    if (file) {
      updateField('imageUrl', '');
    }
  };

  const handleImageUrlChange = (value: string) => {
    // Only discard a selected file when the user actually types a URL.
    // Clearing the field must not wipe a pending upload.
    if (value.trim()) {
      onCoverFileChange(null);
      if (coverInputRef.current) coverInputRef.current.value = '';
    }
    updateField('imageUrl', value);
  };

  const handleImagePositionChange = (position: NewsImagePosition) => {
    onChange({ ...formData, imagePosition: position });
  };

  const handleAttachmentsChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []);
    if (files.length === 0) return;
    onPendingAttachmentsChange([...pendingAttachments, ...files]);
    e.target.value = '';
  };

  const removePendingAttachment = (index: number) => {
    onPendingAttachmentsChange(pendingAttachments.filter((_, i) => i !== index));
  };

  const handleDeleteExisting = async (attachmentId: string) => {
    if (!onDeleteAttachment) return;
    setDeletingAttachmentId(attachmentId);
    try {
      await onDeleteAttachment(attachmentId);
    } finally {
      setDeletingAttachmentId(null);
    }
  };

  const enIncomplete = isEnglishContentIncomplete([
    [formData.title, formData.titleEn],
    [formData.excerpt, formData.excerptEn],
    [formData.content, formData.contentEn],
  ]);

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
            {submitting ? t('admin.common.loading') : submitLabel}
          </Button>
        }
        languageTabs={
          <AdminLanguageTabs
            enIncomplete={enIncomplete}
            frPanel={
              <div className="grid grid-cols-1 gap-6">
                <Field label={t('admin.common.title')} htmlFor="title" required>
                  <input
                    type="text"
                    id="title"
                    value={formData.title}
                    onChange={(e) => updateField('title', e.target.value)}
                    required
                    className={inputClasses}
                  />
                </Field>
                <Field label={t('admin.news.excerpt')} htmlFor="excerpt">
                  <textarea
                    id="excerpt"
                    value={formData.excerpt || ''}
                    onChange={(e) => updateField('excerpt', e.target.value)}
                    rows={2}
                    className={inputClasses}
                  />
                </Field>
                <Field label={t('admin.news.content')} htmlFor="content" required>
                  <RichTextEditor id="content" value={formData.content} onChange={(value) => updateField('content', value)} required label={t('admin.news.content')} minHeight={360} />
                </Field>
              </div>
            }
            enPanel={
              <div className="grid grid-cols-1 gap-6">
                <Field label={t('admin.common.title')} htmlFor="titleEn">
                  <input
                    type="text"
                    id="titleEn"
                    value={formData.titleEn || ''}
                    onChange={(e) => updateField('titleEn', e.target.value)}
                    className={inputClasses}
                    placeholder={t('admin.news.titleEnPlaceholder')}
                  />
                </Field>
                <Field label={t('admin.news.excerpt')} htmlFor="excerptEn">
                  <textarea
                    id="excerptEn"
                    value={formData.excerptEn || ''}
                    onChange={(e) => updateField('excerptEn', e.target.value)}
                    rows={2}
                    className={inputClasses}
                    placeholder={t('admin.news.excerptEnPlaceholder')}
                  />
                </Field>
                <Field label={t('admin.news.content')} htmlFor="contentEn">
                  <RichTextEditor id="contentEn" value={formData.contentEn || ''} onChange={(value) => updateField('contentEn', value)} label={t('admin.news.content')} placeholder={t('admin.news.contentEnPlaceholder')} minHeight={360} />
                </Field>
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
              <Field label={t('admin.news.category')} htmlFor="category">
                <select
                  id="category"
                  value={formData.category || ''}
                  onChange={(e) => updateField('category', e.target.value)}
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="">{t('admin.news.selectCategory')}</option>
                  {NEWS_CATEGORIES.map((category) => (
                    <option key={category} value={category}>
                      {t(getNewsCategoryLabelKey(category) ?? category)}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label={t('admin.news.author')} htmlFor="author">
                <input
                  type="text"
                  id="author"
                  value={formData.author || ''}
                  onChange={(e) => updateField('author', e.target.value)}
                  className={inputClasses}
                />
              </Field>

              <Field label={t('admin.news.publishedDate')} htmlFor="publishedDate">
                <input
                  type="datetime-local"
                  id="publishedDate"
                  value={formData.publishedDate ? formData.publishedDate.slice(0, 16) : ''}
                  onChange={(e) =>
                    updateField(
                      'publishedDate',
                      e.target.value ? new Date(e.target.value).toISOString() : '',
                    )
                  }
                  className={inputClasses}
                />
              </Field>

              <Field label={t('admin.common.status')} htmlFor="status">
                <select
                  id="status"
                  value={formData.status}
                  onChange={(e) => updateField('status', e.target.value)}
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="published">{t('admin.news.statusPublished')}</option>
                  <option value="draft">{t('admin.news.statusDraft')}</option>
                </select>
              </Field>

              <div className="md:col-span-2">
                <label htmlFor="isPinned" className="flex min-h-[44px] cursor-pointer items-center gap-3">
                  <input
                    type="checkbox"
                    id="isPinned"
                    checked={formData.isPinned ?? false}
                    onChange={(e) => updateField('isPinned', e.target.checked)}
                    className="h-5 w-5 rounded-control-sm border border-outline accent-green"
                  />
                  <span className="text-body-md text-ink">{t('admin.news.isPinned')}</span>
                </label>
              </div>
            </div>
          </div>
        }
        aside={
          <>
            <div className="border border-line bg-surface p-6">
              <h2 className="mb-1 text-label-md uppercase text-ink-variant">{t('admin.news.coverImage')}</h2>
              <p className="mb-4 text-body-md text-ink-variant">{t('admin.news.coverImageHint')}</p>

              {coverPreviewUrl ? (
                <img
                  src={coverPreviewUrl}
                  alt=""
                  className={`mb-4 aspect-square w-full border border-line object-cover ${newsImageObjectPositionClass(imagePosition)}`}
                />
              ) : (
                <div className="mb-4 flex aspect-square w-full items-center justify-center border border-line bg-surface-container text-ink-variant">
                  <i className="ri-image-add-line text-4xl" aria-hidden="true"></i>
                </div>
              )}

              <div className="flex flex-wrap items-center gap-2">
                <Button type="button" variant="secondary" onClick={() => coverInputRef.current?.click()} disabled={submitting}>
                  {t('admin.news.uploadCover')}
                </Button>
                {hasCover && (
                  <Button
                    type="button"
                    variant="tertiary"
                    disabled={submitting}
                    onClick={() => {
                      onCoverFileChange(null);
                      updateField('imageUrl', '');
                      if (coverInputRef.current) coverInputRef.current.value = '';
                    }}
                  >
                    {t('admin.news.removeCover')}
                  </Button>
                )}
              </div>
              {coverFile && (
                <p className="mt-2 text-body-md text-green">
                  <i className="ri-checkbox-circle-line mr-1" aria-hidden="true"></i>
                  {coverFile.name}
                </p>
              )}
              <input
                ref={coverInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp,image/gif"
                className="hidden"
                onChange={handleCoverChange}
              />

              {hasCover && (
                <div className="mt-4">
                  <p className="mb-1 text-label-md uppercase text-ink-variant">{t('admin.news.imagePosition')}</p>
                  <p className="mb-3 text-body-md text-ink-variant">{t('admin.news.imagePositionHint')}</p>
                  <div className="flex flex-wrap gap-2" role="group" aria-label={t('admin.news.imagePosition')}>
                    {NEWS_IMAGE_POSITIONS.map((position) => {
                      const selected = imagePosition === position;
                      return (
                        <button
                          key={position}
                          type="button"
                          onClick={() => handleImagePositionChange(position)}
                          disabled={submitting}
                          className={`min-h-[44px] px-4 py-2 text-label-md uppercase transition-colors ${
                            selected
                              ? 'bg-green text-white'
                              : 'border border-line bg-surface text-ink-variant hover:text-green'
                          }`}
                        >
                          {t(`admin.news.imagePosition.${position}`)}
                        </button>
                      );
                    })}
                  </div>
                </div>
              )}

              <div className="mt-4">
                <Field label={t('admin.news.imageUrl')} htmlFor="imageUrl">
                  <input
                    type="text"
                    inputMode="url"
                    id="imageUrl"
                    value={formData.imageUrl || ''}
                    onChange={(e) => handleImageUrlChange(e.target.value)}
                    placeholder="https://"
                    className={inputClasses}
                  />
                </Field>
              </div>
            </div>

            <div className="border border-line bg-surface p-6">
              <h2 className="mb-1 text-label-md uppercase text-ink-variant">{t('admin.news.attachments')}</h2>
              <p className="mb-4 text-body-md text-ink-variant">{t('admin.news.attachmentsHint')}</p>

              {existingAttachments.length > 0 && (
                <ul className="mb-3 space-y-2">
                  {existingAttachments.map((attachment) => (
                    <li
                      key={attachment.id}
                      className="flex items-center justify-between gap-3 border border-line px-3 py-2"
                    >
                      <a
                        href={resolveMediaUrl(attachment.url)}
                        target="_blank"
                        rel="noreferrer"
                        className="min-w-0 flex-1 truncate text-body-md font-semibold text-green hover:underline"
                      >
                        <i className="ri-attachment-2 mr-2" aria-hidden="true"></i>
                        {attachment.fileName}
                        <span className="ml-2 text-ink-variant">({formatFileSize(attachment.sizeBytes)})</span>
                      </a>
                      {onDeleteAttachment && (
                        <button
                          type="button"
                          onClick={() => handleDeleteExisting(attachment.id)}
                          disabled={submitting || deletingAttachmentId === attachment.id}
                          className="text-body-md font-semibold text-red-link transition-colors hover:text-green disabled:opacity-50"
                        >
                          {t('admin.common.delete')}
                        </button>
                      )}
                    </li>
                  ))}
                </ul>
              )}

              {pendingAttachments.length > 0 && (
                <ul className="mb-3 space-y-2">
                  {pendingAttachments.map((file, index) => (
                    <li
                      key={`${file.name}-${index}`}
                      className="flex items-center justify-between gap-3 border border-line px-3 py-2"
                    >
                      <span className="min-w-0 flex-1 truncate text-body-md text-ink">
                        <i className="ri-upload-2-line mr-2 text-green" aria-hidden="true"></i>
                        {file.name}
                        <span className="ml-2 text-ink-variant">({formatFileSize(file.size)})</span>
                      </span>
                      <button
                        type="button"
                        onClick={() => removePendingAttachment(index)}
                        disabled={submitting}
                        className="text-body-md text-red-link transition-colors hover:text-green"
                      >
                        {t('admin.common.delete')}
                      </button>
                    </li>
                  ))}
                </ul>
              )}

              <Button type="button" variant="secondary" onClick={() => attachmentInputRef.current?.click()} disabled={submitting}>
                <i className="ri-attachment-line" aria-hidden="true"></i>
                {t('admin.news.addAttachments')}
              </Button>
              <input
                ref={attachmentInputRef}
                type="file"
                multiple
                accept=".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png,.webp,.gif,application/pdf,image/*"
                className="hidden"
                onChange={handleAttachmentsChange}
              />
            </div>
          </>
        }
      />
    </form>
  );
};
