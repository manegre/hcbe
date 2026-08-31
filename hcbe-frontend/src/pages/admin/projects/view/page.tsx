import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { projectsApi } from '../../../../lib/api/projects';
import type { Project } from '../../../../lib/api/types';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState, Tag } from '../../../../components/ui';

const ViewProjectPage = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [project, setProject] = useState<Project | null>(null);

  useEffect(() => {
    if (id) {
      loadProject();
    }
  }, [id]);

  const loadProject = async () => {
    if (!id) return;

    try {
      setLoading(true);
      const response = await projectsApi.getProjectForAdmin(id);
      setProject(response.data);
    } catch (err: any) {
      console.error('Error loading project:', err);
      setError(err.message || 'Failed to load project');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!project || !id) return;

    if (!confirm(`Are you sure you want to delete "${project.title}"?`)) return;

    try {
      await projectsApi.deleteProject(id);
      navigate('/admin/projects');
    } catch (err: any) {
      console.error('Error deleting project:', err);
      setError(err.message || 'Failed to delete project');
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return '';
    return new Date(dateString).toLocaleDateString('fr-FR');
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error || !project) {
    return (
      <EmptyState
        tone="error"
        title={error || 'Project not found'}
        action={
          <Button to="/admin/projects" variant="secondary">
            Back to Projects
          </Button>
        }
      />
    );
  }

  return (
    <AdminDetailLayout
      title={project.title}
      subtitle={`${project.location} • ${project.type}`}
      backPath="/admin/projects"
      status={{
        status: project.isActive ? 'published' : 'draft',
        label: project.isActive ? 'Active' : 'Inactive',
      }}
      secondaryActions={<Tag>{project.status}</Tag>}
      actions={
        <>
          <Button to={`/admin/projects/${project.id}/edit`} variant="secondary">
            Edit
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            Delete
          </Button>
        </>
      }
      main={
        <>
          {error && <div className="border border-error bg-surface px-4 py-3 text-error">{error}</div>}

          {project.imageUrl ? (
            <img
              src={project.imageUrl}
              alt={project.title}
              className="h-64 w-full border border-line object-cover"
            />
          ) : (
            <div className="flex h-64 w-full items-center justify-center border border-line bg-surface-container">
              <div className="text-center text-ink-variant">
                <i className="ri-image-line mb-2 block text-4xl"></i>
                <div className="text-body-md">No image</div>
              </div>
            </div>
          )}

          <div className="border border-line bg-surface p-6">
            <div className="mb-2 flex items-center justify-between">
              <span className="text-label-md uppercase text-ink-variant">Progress</span>
              <span className="font-display text-headline-sm text-green">{project.progress}%</span>
            </div>
            <div className="h-3 w-full border border-line bg-surface-container">
              <div className="h-full bg-green" style={{ width: `${project.progress}%` }}></div>
            </div>
          </div>

          <div>
            <h2 className="font-display text-headline-sm text-green">Description</h2>
            <p className="mt-3 text-body-md text-ink-variant">{project.description}</p>
          </div>

          <div>
            <h2 className="font-display text-headline-sm text-green">Key Information</h2>
            <DetailList>
              <DetailRow label="Budget Total" value={project.budget} />
              <DetailRow label="Fonds Collectés" value={project.fundsRaised} />
              <DetailRow label="Bénéficiaires" value={project.beneficiaries} />
              <DetailRow label="Type" value={project.type} />
            </DetailList>
          </div>

          {(project.startDate || project.endDate) && (
            <div>
              <h2 className="font-display text-headline-sm text-green">Timeline</h2>
              <DetailList>
                {project.startDate && <DetailRow label="Date de début" value={formatDate(project.startDate)} />}
                {project.endDate && <DetailRow label="Date de fin" value={formatDate(project.endDate)} />}
              </DetailList>
            </div>
          )}

          {project.partners.length > 0 && (
            <div>
              <h2 className="font-display text-headline-sm text-green">Partenaires</h2>
              <div className="mt-3 flex flex-wrap gap-2">
                {project.partners.map((partner, idx) => (
                  <Tag key={idx}>{partner}</Tag>
                ))}
              </div>
            </div>
          )}

          <div>
            <h2 className="font-display text-headline-sm text-green">Metadata</h2>
            <DetailList>
              <DetailRow label="Created" value={formatDate(project.createdAt)} />
              <DetailRow label="Updated" value={formatDate(project.updatedAt)} />
              <DetailRow label="Status" value={project.isActive ? 'Active' : 'Inactive'} />
              <DetailRow label="Project ID" value={<span className="font-mono">{project.id}</span>} />
            </DetailList>
          </div>
        </>
      }
    />
  );
};

export default ViewProjectPage;
