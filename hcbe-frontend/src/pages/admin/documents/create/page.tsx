import React, { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AdminLanguageTabs,
  isEnglishContentIncomplete,
} from '../../../../components/admin/AdminLanguageTabs';
import { AdminFormLayout } from '../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses, RichTextEditor } from '../../../../components/ui';
import { buildApiUrl } from '../../../../lib/api/base-url';

export const CreateDocumentPage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    nameEn: '',
    description: '',
    descriptionEn: '',
    icon: 'ri-file-line',
    pages: '',
    pagesEn: '',
    category: 'officiel',
    categoryEn: '',
    displayOrder: 0,
  });
  const [file, setFile] = useState<File | null>(null);

  const backPath = '/admin/documents';

  const iconOptions = [
    { value: 'ri-file-text-line', label: 'Texte' },
    { value: 'ri-book-line', label: 'Livre' },
    { value: 'ri-shield-check-line', label: 'Bouclier' },
    { value: 'ri-roadmap-line', label: 'Roadmap' },
    { value: 'ri-file-pdf-line', label: 'PDF' },
    { value: 'ri-article-line', label: 'Article' },
    { value: 'ri-folder-line', label: 'Dossier' },
  ];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!file) {
      alert('Veuillez sélectionner un fichier');
      return;
    }

    setIsSubmitting(true);

    try {
      const token = localStorage.getItem('hcbe_token');
      const data = new FormData();
      data.append('file', file);
      data.append('name', formData.name);
      data.append('description', formData.description);
      data.append('icon', formData.icon);
      data.append('pages', formData.pages);
      data.append('category', formData.category);
      data.append('displayOrder', formData.displayOrder.toString());
      data.append('nameEn', formData.nameEn);
      data.append('descriptionEn', formData.descriptionEn);
      data.append('pagesEn', formData.pagesEn);
      data.append('categoryEn', formData.categoryEn);

      const response = await fetch(buildApiUrl('/api/documents'), {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
        },
        body: data,
      });

      const result = await response.json();

      if (result.success) {
        navigate(backPath);
      } else {
        alert('Erreur lors de la création du document: ' + (result.message || 'Erreur inconnue'));
      }
    } catch (error) {
      console.error('Error creating document:', error);
      alert('Erreur lors de la création du document');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile) {
      setFile(selectedFile);
    }
  };

  const enIncomplete = isEnglishContentIncomplete([
    [formData.name, formData.nameEn],
    [formData.description, formData.descriptionEn],
    [formData.pages, formData.pagesEn],
    [formData.category, formData.categoryEn],
  ]);

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title={t('admin.documents.create')}
        backPath={backPath}
        backLabel={t('admin.common.backToList')}
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? t('admin.common.loading') : t('admin.documents.create')}
          </Button>
        }
        languageTabs={
          <AdminLanguageTabs
            enIncomplete={enIncomplete}
            frPanel={
              <div className="space-y-6">
                <Field label={t('admin.common.name')} htmlFor="name" required>
                  <input
                    type="text"
                    id="name"
                    required
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className={inputClasses}
                    placeholder="ex: Statuts du HCBE Canada"
                  />
                </Field>
                <Field label={t('admin.common.description')} htmlFor="description">
                  <RichTextEditor
                    id="description"
                    value={formData.description}
                    onChange={(description) => setFormData((current) => ({ ...current, description }))}
                    placeholder="Description du document..."
                    minHeight={240}
                    label={t('admin.common.description')}
                  />
                </Field>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <Field label={t('admin.documents.colPages')} htmlFor="pages">
                    <input
                      type="text"
                      id="pages"
                      value={formData.pages}
                      onChange={(e) => setFormData({ ...formData, pages: e.target.value })}
                      className={inputClasses}
                      placeholder="ex: 24 pages"
                    />
                  </Field>
                  <Field label={t('admin.news.category')} htmlFor="category">
                    <input
                      type="text"
                      id="category"
                      value={formData.category}
                      onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                      className={inputClasses}
                      placeholder="ex: officiel"
                    />
                  </Field>
                </div>
              </div>
            }
            enPanel={
              <div className="space-y-6">
                <Field label={t('admin.common.name')} htmlFor="nameEn">
                  <input
                    type="text"
                    id="nameEn"
                    value={formData.nameEn}
                    onChange={(e) => setFormData({ ...formData, nameEn: e.target.value })}
                    className={inputClasses}
                    placeholder="e.g. HCBE Canada Bylaws"
                  />
                </Field>
                <Field label={t('admin.common.description')} htmlFor="descriptionEn">
                  <RichTextEditor
                    id="descriptionEn"
                    value={formData.descriptionEn}
                    onChange={(descriptionEn) => setFormData((current) => ({ ...current, descriptionEn }))}
                    minHeight={240}
                    label={t('admin.common.description')}
                  />
                </Field>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <Field label={t('admin.documents.colPages')} htmlFor="pagesEn">
                    <input
                      type="text"
                      id="pagesEn"
                      value={formData.pagesEn}
                      onChange={(e) => setFormData({ ...formData, pagesEn: e.target.value })}
                      className={inputClasses}
                      placeholder="e.g. 24 pages"
                    />
                  </Field>
                  <Field label={t('admin.news.category')} htmlFor="categoryEn">
                    <input
                      type="text"
                      id="categoryEn"
                      value={formData.categoryEn}
                      onChange={(e) => setFormData({ ...formData, categoryEn: e.target.value })}
                      className={inputClasses}
                      placeholder="e.g. official"
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
            <div className="space-y-6">
              <Field label={t('admin.documents.file')} htmlFor="file" required>
                <div className="flex flex-wrap items-center gap-3">
                  <Button type="button" variant="secondary" onClick={() => fileInputRef.current?.click()}>
                    {t('admin.documents.uploadFile')}
                  </Button>
                  {file ? (
                    <p className="break-all text-body-md text-green">
                      <i className="ri-file-line mr-1" aria-hidden="true" />
                      {file.name}
                    </p>
                  ) : (
                    <p className="text-body-md text-ink-variant">{t('admin.documents.fileHint')}</p>
                  )}
                </div>
                <input
                  ref={fileInputRef}
                  id="file"
                  name="file"
                  type="file"
                  className="sr-only"
                  accept=".pdf,.doc,.docx,.xls,.xlsx"
                  onChange={handleFileChange}
                  required
                />
              </Field>

              <Field label={t('admin.documents.icon')} htmlFor="icon">
                <select
                  id="icon"
                  value={formData.icon}
                  onChange={(e) => setFormData({ ...formData, icon: e.target.value })}
                  className={`${inputClasses} cursor-pointer`}
                >
                  {iconOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label={t('admin.common.order')} htmlFor="displayOrder">
                <input
                  type="number"
                  id="displayOrder"
                  value={formData.displayOrder}
                  onChange={(e) =>
                    setFormData({ ...formData, displayOrder: parseInt(e.target.value) })
                  }
                  className={inputClasses}
                />
              </Field>
            </div>
          </div>
        }
      />
    </form>
  );
};

export default CreateDocumentPage;
