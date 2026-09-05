import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useNavigate } from 'react-router-dom';
import { buildApiUrl } from '../../../../lib/api/base-url';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState, RichTextContent, Tag } from '../../../../components/ui';
import { AdminStatCard } from '../../../../components/admin/AdminStatCard';

interface Document {
  id: string;
  name: string;
  description?: string;
  icon?: string;
  type?: string;
  size?: string;
  pages?: string;
  category?: string;
  url?: string;
  downloads: number;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
}

export const ViewDocumentPage: React.FC = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language.startsWith('en') ? 'en-CA' : 'fr-CA';
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [document, setDocument] = useState<Document | null>(null);
  const [isLoading, setIsLoading] = useState(true);

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
      }
    } catch (error) {
      console.error('Error loading document:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!document || !window.confirm(t('admin.documents.confirmDelete', { name: document.name }))) {
      return;
    }

    try {
      const token = localStorage.getItem('hcbe_token');
      const response = await fetch(buildApiUrl(`/api/documents/${document.id}`), {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.ok) {
        navigate('/admin/documents');
      }
    } catch (error) {
      console.error('Error deleting document:', error);
      alert(t('admin.documents.errorDelete'));
    }
  };

  const handleDownload = async () => {
    if (!document) return;

    try {
      const token = localStorage.getItem('hcbe_token');
      const response = await fetch(buildApiUrl(`/api/documents/admin/${document.id}/download`), {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = window.document.createElement('a');
        a.href = url;
        const contentDisposition = response.headers.get('content-disposition');
        const filename = contentDisposition
          ? contentDisposition.split('filename=')[1].replace(/"/g, '')
          : 'document.pdf';
        a.download = filename;
        window.document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        window.document.body.removeChild(a);
      }
    } catch (error) {
      console.error('Error downloading document:', error);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (!document) {
    return (
      <EmptyState
        tone="error"
        title={t('admin.documents.notFound')}
        action={
          <Button to="/admin/documents" variant="secondary">
            {t('admin.common.backToList')}
          </Button>
        }
      />
    );
  }

  return (
    <AdminDetailLayout
      title={document.name}
      backPath="/admin/documents"
      backLabel={t('admin.common.backToList')}
      status={{
        status: document.isActive ? 'published' : 'draft',
        label: document.isActive ? t('admin.common.active') : t('admin.common.inactive'),
      }}
      secondaryActions={
        <div className="flex flex-wrap items-center gap-2">
          {document.icon && <i className={`${document.icon} text-2xl text-green`} aria-hidden="true"></i>}
          {document.category && <Tag>{document.category}</Tag>}
        </div>
      }
      actions={
        <>
          <Button to={`/admin/documents/${document.id}/edit`} variant="secondary">
            <i className="ri-edit-line" aria-hidden="true" />
            {t('admin.common.edit')}
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            <i className="ri-delete-bin-line" aria-hidden="true" />
            {t('admin.common.delete')}
          </Button>
        </>
      }
      main={
        <>
          {document.description && (
            <div>
              <h2 className="font-display text-headline-sm text-green">{t('admin.common.description')}</h2>
              <RichTextContent value={document.description} className="mt-3 text-body-md text-ink-variant" />
            </div>
          )}

          <div className="grid grid-cols-2 gap-3 xl:grid-cols-4">
            <AdminStatCard value={document.size || 'N/A'} label={t('admin.documents.colSize')} icon="ri-hard-drive-3-line" />
            <AdminStatCard value={document.pages || 'N/A'} label={t('admin.documents.colPages')} icon="ri-pages-line" tone="neutral" />
            <AdminStatCard value={document.downloads} label={t('admin.documents.colDownloads')} icon="ri-download-cloud-2-line" tone="gold" />
            <AdminStatCard value={document.displayOrder} label={t('admin.common.order')} icon="ri-sort-number-asc" tone="neutral" />
          </div>

          <div>
            <h2 className="font-display text-headline-sm text-green">{t('admin.documents.technicalInfo')}</h2>
            <DetailList>
              <DetailRow label="ID" value={<span className="font-mono">{document.id}</span>} />
              {document.type && <DetailRow label={t('admin.documents.fileType')} value={document.type} />}
              <DetailRow
                label={t('admin.documents.createdAt')}
                value={new Date(document.createdAt).toLocaleDateString(locale, {
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit',
                })}
              />
              {document.url && <DetailRow label="URL" value={document.url} />}
            </DetailList>
          </div>

          {document.url && (
            <Button variant="primary" onClick={handleDownload} className="w-full justify-center rounded-xl">
              <i className="ri-download-line" aria-hidden="true" />
              {t('admin.documents.download')}
            </Button>
          )}
        </>
      }
    />
  );
};

export default ViewDocumentPage;
