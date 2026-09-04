import { projectsApi } from '../../../lib/api/projects';
import type { Project } from '../../../lib/api/types';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { Field, Tag, Td, inputClasses } from '../../../components/ui';

const AdminProjectsList = () => {
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const { t } = useTranslation();

  useEffect(() => {
    loadProjects();
  }, []);

  const loadProjects = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await projectsApi.getProjectsForAdmin();
      setProjects(response.data);
    } catch (err) {
      console.error('Error loading projects:', err);
      setError(t('admin.projects.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm(t('admin.common.confirmDeleteGeneric'))) return;

    try {
      await projectsApi.deleteProject(id);
      setProjects(projects.filter(p => p.id !== id));
    } catch (err) {
      console.error('Error deleting project:', err);
      setError(t('admin.projects.errorDelete'));
    }
  };

  const filteredProjects = projects.filter(project => {
    if (statusFilter !== 'all' && project.status !== statusFilter) return false;
    if (typeFilter !== 'all' && project.type !== typeFilter) return false;
    return true;
  });

  const toolbar = (
    <>
      <Field label={t('admin.common.status')} htmlFor="project-status">
        <select
          id="project-status"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className={inputClasses}
        >
          <option value="all">{t('admin.projects.filterAllStatus')}</option>
          <option value="En cours">{t('public.engagement.projets.status.En cours')}</option>
          <option value="Actif">{t('public.engagement.projets.status.Actif')}</option>
          <option value="Planification">{t('public.engagement.projets.status.Planification')}</option>
          <option value="Terminé">{t('public.engagement.projets.status.Terminé')}</option>
        </select>
      </Field>
      <Field label={t('admin.common.type')} htmlFor="project-type">
        <select
          id="project-type"
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value)}
          className={inputClasses}
        >
          <option value="all">{t('admin.projects.filterAllTypes')}</option>
          <option value="Développement au Burkina">{t('public.engagement.projets.type.Développement au Burkina')}</option>
          <option value="Initiative Locale">{t('public.engagement.projets.type.Initiative Locale')}</option>
        </select>
      </Field>
    </>
  );

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <AdminListPage
      title={t('admin.projects.title')}
      count={error ? undefined : filteredProjects.length}
      createLabel={t('admin.projects.create')}
      createPath="/admin/projects/create"
      toolbar={toolbar}
      columns={[
        { key: 'project', label: t('admin.projects.colProject') },
        { key: 'status', label: t('admin.common.status') },
        { key: 'type', label: t('admin.common.type') },
        { key: 'progress', label: t('admin.projects.colProgress') },
        { key: 'budget', label: t('admin.projects.colBudget') },
        { key: 'actions', label: t('admin.common.actions'), align: 'right' },
      ]}
      isEmpty={filteredProjects.length === 0}
      emptyTitle={t('admin.projects.emptyTitle')}
      emptyDescription={
        statusFilter !== 'all' || typeFilter !== 'all'
          ? t('admin.projects.emptyFilter')
          : t('admin.projects.emptyAll')
      }
      error={error ?? undefined}
      onRetry={loadProjects}
    >
      {filteredProjects.map((project) => (
        <tr
          key={project.id}
          className={`transition-colors hover:bg-surface-container ${!project.isActive ? 'opacity-60' : ''}`}
        >
          <Td className="text-ink">
            <div className="font-medium">{project.title}</div>
            <div className="text-ink-variant">{project.location}</div>
            <div className="text-ink-variant">{project.beneficiaries}</div>
          </Td>
          <Td>
            <Tag>{project.status}</Tag>
            {!project.isActive && (
              <div className="mt-1 text-body-md text-error">{t('admin.common.inactive')}</div>
            )}
          </Td>
          <Td>{project.type}</Td>
          <Td>
            <div className="mb-1">{project.progress}%</div>
            <div className="h-2 w-24 bg-surface-container">
              <div className="h-2 bg-green" style={{ width: `${project.progress}%` }} />
            </div>
          </Td>
          <Td>
            <div>{project.budget}</div>
            <div className="text-green">{t('admin.projects.raised', { amount: project.fundsRaised })}</div>
          </Td>
          <Td align="right">
            <div className="inline-flex items-center justify-end gap-1">
              <Link
                to={`/admin/projects/${project.id}`}
                aria-label={t('admin.common.view')}
                title={t('admin.common.view')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-eye-line text-lg" aria-hidden="true" />
              </Link>
              <Link
                to={`/admin/projects/${project.id}/edit`}
                aria-label={t('admin.common.edit')}
                title={t('admin.common.edit')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-edit-line text-lg" aria-hidden="true" />
              </Link>
              <button
                type="button"
                onClick={() => handleDelete(project.id)}
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
  );
};

export default AdminProjectsList;
