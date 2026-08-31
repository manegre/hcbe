import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { buildApiUrl } from '../../../../lib/api/base-url';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState, Tag } from '../../../../components/ui';
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
    if (!document || !window.confirm(`Êtes-vous sûr de vouloir supprimer "${document.name}" ?`)) {
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
      alert('Erreur lors de la suppression du document');
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
        title="Document non trouvé"
        action={
          <Button to="/admin/documents" variant="secondary">
            Retour à la liste
          </Button>
        }
      />
    );
  }

  return (
    <AdminDetailLayout
      title={document.name}
      backPath="/admin/documents"
      backLabel="Retour à la liste"
      status={{
        status: document.isActive ? 'published' : 'draft',
        label: document.isActive ? 'Actif' : 'Inactif',
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
            Modifier
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            <i className="ri-delete-bin-line" aria-hidden="true" />
            Supprimer
          </Button>
        </>
      }
      main={
        <>
          {document.description && (
            <div>
              <h2 className="font-display text-headline-sm text-green">Description</h2>
              <p className="mt-3 text-body-md text-ink-variant">{document.description}</p>
            </div>
          )}

          <div className="grid grid-cols-2 gap-3 xl:grid-cols-4">
            <AdminStatCard value={document.size || 'N/A'} label="Taille" icon="ri-hard-drive-3-line" />
            <AdminStatCard value={document.pages || 'N/A'} label="Pages" icon="ri-pages-line" tone="neutral" />
            <AdminStatCard value={document.downloads} label="Téléchargements" icon="ri-download-cloud-2-line" tone="gold" />
            <AdminStatCard value={document.displayOrder} label="Ordre" icon="ri-sort-number-asc" tone="neutral" />
          </div>

          <div>
            <h2 className="font-display text-headline-sm text-green">Informations techniques</h2>
            <DetailList>
              <DetailRow label="ID" value={<span className="font-mono">{document.id}</span>} />
              {document.type && <DetailRow label="Type de fichier" value={document.type} />}
              <DetailRow
                label="Date de création"
                value={new Date(document.createdAt).toLocaleDateString('fr-FR', {
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
              Télécharger le document
            </Button>
          )}
        </>
      }
    />
  );
};

export default ViewDocumentPage;
