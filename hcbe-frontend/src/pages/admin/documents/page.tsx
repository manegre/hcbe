import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { buildApiUrl } from '../../../lib/api/base-url';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { Field, StatusChip, Td, inputClasses } from '../../../components/ui';

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

export const AdminDocumentsList: React.FC = () => {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('all');
  const { t } = useTranslation();

  useEffect(() => {
    loadDocuments();
  }, []);

  const loadDocuments = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const token = localStorage.getItem('hcbe_token');
      const response = await fetch(buildApiUrl('/api/documents/admin'), {
        headers: { Authorization: `Bearer ${token}` },
      });
      const data = await response.json();
      if (data.success && data.data) {
        setDocuments(data.data);
      } else {
        setError(t('admin.documents.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading documents:', err);
      setError(err instanceof Error ? err.message : t('admin.documents.errorLoad'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteDocument = async (id: string, name: string) => {
    if (!window.confirm(t('admin.common.confirmDelete', { name }))) {
      return;
    }

    try {
      const token = localStorage.getItem('hcbe_token');
      const response = await fetch(buildApiUrl(`/api/documents/${id}`), {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (response.ok) {
        setDocuments(documents.filter(doc => doc.id !== id));
      }
    } catch (error) {
      console.error('Error deleting document:', error);
      alert(t('admin.documents.errorDelete'));
    }
  };

  const handleToggleActive = async (id: string, currentStatus: boolean) => {
    try {
      const token = localStorage.getItem('hcbe_token');
      const formData = new FormData();
      formData.append('isActive', (!currentStatus).toString());

      const response = await fetch(buildApiUrl(`/api/documents/${id}`), {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`
        },
        body: formData
      });

      if (response.ok) {
        setDocuments(documents.map(doc =>
          doc.id === id ? { ...doc, isActive: !currentStatus } : doc
        ));
      }
    } catch (error) {
      console.error('Error updating document:', error);
    }
  };

  const filteredDocuments = documents.filter(doc => {
    if (filter === 'all') return true;
    if (filter === 'active') return doc.isActive;
    if (filter === 'inactive') return !doc.isActive;
    return doc.category === filter;
  });

  const sortedDocuments = [...filteredDocuments].sort((a, b) => a.displayOrder - b.displayOrder);

  const categories = [...new Set(documents.map(doc => doc.category).filter(Boolean))];
  const filterOptions = [
    { value: 'all', label: t('admin.documents.filterAll') },
    { value: 'active', label: t('admin.documents.filterActive') },
    { value: 'inactive', label: t('admin.documents.filterInactive') },
    ...categories.map((cat) => ({ value: cat!, label: cat! })),
  ];

  const toolbar = (
    <Field label={t('admin.common.filterBy')} htmlFor="document-filter">
      <select
        id="document-filter"
        value={filter}
        onChange={(e) => setFilter(e.target.value)}
        className={inputClasses}
      >
        {filterOptions.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </Field>
  );

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <>
      <AdminListPage
        title={t('admin.documents.title')}
        count={error ? undefined : sortedDocuments.length}
        createLabel={t('admin.documents.create')}
        createPath="/admin/documents/create"
        toolbar={toolbar}
        columns={[
          { key: 'document', label: t('admin.documents.colDocument') },
          { key: 'size', label: t('admin.documents.colSize') },
          { key: 'pages', label: t('admin.documents.colPages') },
          { key: 'downloads', label: t('admin.documents.colDownloads') },
          { key: 'status', label: t('admin.common.status') },
          { key: 'actions', label: t('admin.common.actions'), align: 'right' },
        ]}
        isEmpty={sortedDocuments.length === 0}
        emptyTitle={t('admin.documents.emptyTitle')}
        emptyDescription={
          filter === 'all' ? t('admin.documents.emptyAll') : t('admin.documents.emptyFilter')
        }
        error={error ?? undefined}
      >
        {sortedDocuments.map((doc) => (
          <tr key={doc.id} className="transition-colors hover:bg-surface-container">
            <Td className="text-ink">
              <div className="flex items-center gap-3">
                <i className={`${doc.icon || 'ri-file-line'} text-lg text-ink-variant`} aria-hidden="true" />
                <div>
                  <div className="font-medium">{doc.name}</div>
                  {doc.description && (
                    <div className="mt-1 max-w-xs truncate text-body-md text-ink-variant">{doc.description}</div>
                  )}
                </div>
              </div>
            </Td>
            <Td>{doc.size || t('admin.common.na')}</Td>
            <Td>{doc.pages || t('admin.common.na')}</Td>
            <Td>{doc.downloads}</Td>
            <Td>
              <button type="button" onClick={() => handleToggleActive(doc.id, doc.isActive)}>
                <StatusChip
                  status={doc.isActive ? 'published' : 'draft'}
                  label={doc.isActive ? t('admin.common.active') : t('admin.common.inactive')}
                />
              </button>
            </Td>
            <Td align="right">
              <div className="inline-flex items-center justify-end gap-1">
                <Link
                  to={`/admin/documents/${doc.id}`}
                  aria-label={t('admin.common.view')}
                  title={t('admin.common.view')}
                  className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
                >
                  <i className="ri-eye-line text-lg" aria-hidden="true" />
                </Link>
                <Link
                  to={`/admin/documents/${doc.id}/edit`}
                  aria-label={t('admin.common.edit')}
                  title={t('admin.common.edit')}
                  className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
                >
                  <i className="ri-edit-line text-lg" aria-hidden="true" />
                </Link>
                <button
                  type="button"
                  onClick={() => handleDeleteDocument(doc.id, doc.name)}
                  aria-label={t('admin.common.delete')}
                  title={t('admin.common.delete')}
                  className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center rounded-control text-error transition-colors hover:text-error-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-error"
                >
                  <i className="ri-delete-bin-line text-lg" aria-hidden="true" />
                </button>
              </div>
            </Td>
          </tr>
        ))}
      </AdminListPage>

      <div className="grid grid-cols-1 gap-gutter sm:grid-cols-2 xl:grid-cols-4">
        <div className="border border-line bg-surface p-6">
          <p className="font-display text-headline-xl tabular-nums text-green">{documents.length}</p>
          <p className="mt-2 text-label-md uppercase text-ink-variant">{t('admin.documents.statsTotal')}</p>
        </div>
        <div className="border border-line bg-surface p-6">
          <p className="font-display text-headline-xl tabular-nums text-green">
            {documents.filter((d) => d.isActive).length}
          </p>
          <p className="mt-2 text-label-md uppercase text-ink-variant">{t('admin.documents.statsActive')}</p>
        </div>
        <div className="border border-line bg-surface p-6">
          <p className="font-display text-headline-xl tabular-nums text-green">
            {documents.reduce((sum, d) => sum + d.downloads, 0)}
          </p>
          <p className="mt-2 text-label-md uppercase text-ink-variant">{t('admin.documents.statsDownloads')}</p>
        </div>
      </div>
    </>
  );
};

export default AdminDocumentsList;
