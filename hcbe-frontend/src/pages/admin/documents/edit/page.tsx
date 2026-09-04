import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AdminLanguageTabs,
  isEnglishContentIncomplete,
} from '../../../../components/admin/AdminLanguageTabs';
import { AdminFormLayout } from '../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../components/ui';
import { buildApiUrl } from '../../../../lib/api/base-url';

const fieldClass = inputClasses;

interface Document {
  id: string;
  name: string;
  nameEn?: string;
  description?: string;
  descriptionEn?: string;
  icon?: string;
  type?: string;
  size?: string;
  pages?: string;
  pagesEn?: string;
  category?: string;
  categoryEn?: string;
  url?: string;
  downloads: number;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
}

export const EditDocumentPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const newFileInputRef = useRef<HTMLInputElement>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [document, setDocument] = useState<Document | null>(null);
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
    isActive: true,
    file: null as File | null,
  });

  const backPath = `/admin/documents/${id}`;

  const iconOptions = [
    { value: 'ri-file-text-line', label: 'Texte' },
    { value: 'ri-book-line', label: 'Livre' },
    { value: 'ri-shield-check-line', label: 'Bouclier' },
    { value: 'ri-roadmap-line', label: 'Roadmap' },
    { value: 'ri-file-pdf-line', label: 'PDF' },
    { value: 'ri-article-line', label: 'Article' },
    { value: 'ri-folder-line', label: 'Dossier' },
  ];

  useEffect(() => {
    if (id) {
      loadDocument(id);
    }
  }, [id]);

  const loadDocument = async (docId: string) => {
    try {
      setIsLoading(true);
      const token = localStorage.getItem('hcbe_token');
      const response = await fetch(buildApiUrl(`/api/documents/admin/${docId}`), {
        headers: { Authorization: `Bearer ${token}` },
      });
      const data = await response.json();
      if (data.success && data.data) {
        setDocument(data.data);
        setFormData({
          name: data.data.name,
          nameEn: data.data.nameEn || '',
          description: data.data.description || '',
          descriptionEn: data.data.descriptionEn || '',
          icon: data.data.icon || 'ri-file-line',
          pages: data.data.pages || '',
          pagesEn: data.data.pagesEn || '',
          category: data.data.category || 'officiel',
          categoryEn: data.data.categoryEn || '',
          displayOrder: data.data.displayOrder,
          isActive: data.data.isActive,
          file: null,
        });
      }
    } catch (error) {
      console.error('Error loading document:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;

    setIsSubmitting(true);

    try {
      const token = localStorage.getItem('hcbe_token');
      const data = new FormData();
      data.append('name', formData.name);
      data.append('description', formData.description);
      data.append('icon', formData.icon);
      data.append('pages', formData.pages);
      data.append('category', formData.category);
      data.append('displayOrder', formData.displayOrder.toString());
      data.append('isActive', formData.isActive.toString());
      data.append('nameEn', formData.nameEn);
      data.append('descriptionEn', formData.descriptionEn);
      data.append('pagesEn', formData.pagesEn);
      data.append('categoryEn', formData.categoryEn);

      if (formData.file) {
        data.append('file', formData.file);
      }

      const response = await fetch(buildApiUrl(`/api/documents/${id}`), {
        method: 'PUT',
        headers: {
          Authorization: `Bearer ${token}`,
        },
        body: data,
      });

      const result = await response.json();

      if (result.success) {
        navigate(`/admin/documents/${id}`);
      } else {
        alert(
          'Erreur lors de la modification du document: ' + (result.message || 'Erreur inconnue'),
        );
      }
    } catch (error) {
      console.error('Error updating document:', error);
      alert('Erreur lors de la modification du document');
    } finally {
      setIsSubmitting(false);
    }
  };

  const enIncomplete = isEnglishContentIncomplete([
    [formData.name, formData.nameEn],
    [formData.description, formData.descriptionEn],
    [formData.pages, formData.pagesEn],
    [formData.category, formData.categoryEn],
  ]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green"></div>
      </div>
    );
  }

  if (!document) {
    return (
      <div className="py-12 text-center">
        <h3 className="text-headline-md text-ink">{t('admin.documents.notFound')}</h3>
        <Link to="/admin/documents" className="mt-4 inline-block text-body-md text-green hover:text-green-deep">
          {t('admin.common.backToList')}
        </Link>
      </div>
    );
  }

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title="Modifier le Document"
        backPath={backPath}
        backLabel="Retour au document"
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? t('admin.common.loading') : t('admin.common.save')}
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
                    className={fieldClass}
                  />
                </Field>
                <Field label={t('admin.common.description')} htmlFor="description">
                  <textarea
                    id="description"
                    rows={3}
                    value={formData.description}
                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                    className={fieldClass}
                  />
                </Field>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <Field label={t('admin.documents.colPages')} htmlFor="pages">
                    <input
                      type="text"
                      id="pages"
                      value={formData.pages}
                      onChange={(e) => setFormData({ ...formData, pages: e.target.value })}
                      className={fieldClass}
                      placeholder="ex: 24 pages"
                    />
                  </Field>
                  <Field label={t('admin.news.category')} htmlFor="category">
                    <input
                      type="text"
                      id="category"
                      value={formData.category}
                      onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                      className={fieldClass}
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
                    className={fieldClass}
                  />
                </Field>
                <Field label={t('admin.common.description')} htmlFor="descriptionEn">
                  <textarea
                    id="descriptionEn"
                    rows={3}
                    value={formData.descriptionEn}
                    onChange={(e) => setFormData({ ...formData, descriptionEn: e.target.value })}
                    className={fieldClass}
                  />
                </Field>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <Field label={t('admin.documents.colPages')} htmlFor="pagesEn">
                    <input
                      type="text"
                      id="pagesEn"
                      value={formData.pagesEn}
                      onChange={(e) => setFormData({ ...formData, pagesEn: e.target.value })}
                      className={fieldClass}
                      placeholder="e.g. 24 pages"
                    />
                  </Field>
                  <Field label={t('admin.news.category')} htmlFor="categoryEn">
                    <input
                      type="text"
                      id="categoryEn"
                      value={formData.categoryEn}
                      onChange={(e) => setFormData({ ...formData, categoryEn: e.target.value })}
                      className={fieldClass}
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
              <Field label={t('admin.documents.icon')} htmlFor="icon">
                <select
                  id="icon"
                  value={formData.icon}
                  onChange={(e) => setFormData({ ...formData, icon: e.target.value })}
                  className={`${fieldClass} cursor-pointer`}
                >
                  {iconOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <div className="mt-2 flex items-center gap-2 text-body-md text-ink-variant">
                  <i className={`${formData.icon} text-2xl text-green`}></i>
                  <span>{t('admin.documents.iconPreview')}</span>
                </div>
              </Field>

              <Field label={t('admin.common.order')} htmlFor="displayOrder">
                <input
                  type="number"
                  id="displayOrder"
                  value={formData.displayOrder}
                  onChange={(e) =>
                    setFormData({ ...formData, displayOrder: parseInt(e.target.value) })
                  }
                  className={fieldClass}
                />
              </Field>

              <label htmlFor="isActive" className="flex min-h-[44px] cursor-pointer items-center gap-3">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="h-5 w-5 rounded-control-sm border border-outline accent-green"
                />
                <span className="text-body-md text-ink">{t('admin.common.active')}</span>
              </label>

              <div className="border border-line bg-surface-container p-4">
                <div className="flex gap-3">
                  <i className="ri-upload-line text-xl text-green" aria-hidden="true" />
                  <div>
                    <h3 className="text-label-md uppercase text-ink">
                      {t('admin.documents.replaceFile')}
                    </h3>
                    <div className="mt-2 text-body-md text-ink-variant">
                      <p>{t('admin.documents.replaceFileHint')}</p>
                      {document.url && <p className="mt-1">{t('admin.documents.currentFile')}: {document.url}</p>}
                    </div>
                  </div>
                </div>
              </div>

              <Field label={t('admin.documents.newFile')} htmlFor="newFile">
                <div className="flex flex-wrap items-center gap-3">
                  <Button type="button" variant="secondary" onClick={() => newFileInputRef.current?.click()}>
                    {t('admin.documents.chooseFile')}
                  </Button>
                  {formData.file && (
                    <p className="break-all text-body-md text-green">
                      <i className="ri-file-line mr-1" aria-hidden="true" />
                      {formData.file.name}
                    </p>
                  )}
                </div>
                <input
                  ref={newFileInputRef}
                  id="newFile"
                  type="file"
                  accept=".pdf,.doc,.docx"
                  className="sr-only"
                  onChange={(e) => {
                    const files = e.target.files;
                    if (files && files.length > 0) {
                      setFormData({ ...formData, file: files[0] });
                    }
                  }}
                />
              </Field>
            </div>
          </div>
        }
      />
    </form>
  );
};

export default EditDocumentPage;
