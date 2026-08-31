import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { teamMembersApi } from '../../../lib/api/team-members';
import type { TeamMemberDto } from '../../../lib/api/types';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { StatusChip, Td } from '../../../components/ui';

const TeamMembersPage: React.FC = () => {
  const [teamMembers, setTeamMembers] = useState<TeamMemberDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { t } = useTranslation();

  const loadTeamMembers = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await teamMembersApi.getAllTeamMembers();
      if (response.success && response.data) {
        setTeamMembers(response.data);
      } else {
        setError(t('admin.team.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading team members:', err);
      setError(t('admin.team.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTeamMembers();
  }, []);

  const handleToggleStatus = async (id: string) => {
    try {
      const response = await teamMembersApi.toggleTeamMemberStatus(id);
      if (response.success) {
        loadTeamMembers(); // Reload list
      }
    } catch (err) {
      console.error('Error toggling team member status:', err);
    }
  };

  const handleDelete = async (id: string) => {
    if (window.confirm(t('admin.common.confirmDeleteGeneric'))) {
      try {
        const response = await teamMembersApi.deleteTeamMember(id);
        if (response.success) {
          loadTeamMembers(); // Reload list
        }
      } catch (err) {
        console.error('Error deleting team member:', err);
      }
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <AdminListPage
      title={t('admin.team.title')}
      count={error ? undefined : teamMembers.length}
      createLabel={t('admin.team.create')}
      createPath="/admin/team-members/create"
      columns={[
        { key: 'member', label: t('admin.team.colMember') },
        { key: 'position', label: t('admin.team.colPosition') },
        { key: 'zone', label: t('admin.common.zone') },
        { key: 'status', label: t('admin.common.status') },
        { key: 'order', label: t('admin.team.colOrder') },
        { key: 'actions', label: t('admin.common.actions'), align: 'right' },
      ]}
      isEmpty={teamMembers.length === 0}
      emptyTitle={t('admin.team.emptyTitle')}
      error={error ?? undefined}
      onRetry={loadTeamMembers}
    >
      {teamMembers.map((member) => (
        <tr key={member.id} className="transition-colors hover:bg-surface-container">
          <Td className="text-ink">
            <div className="font-medium">{member.name}</div>
            <div className="text-ink-variant">{member.email}</div>
          </Td>
          <Td>{member.position}</Td>
          <Td>{member.zone}</Td>
          <Td>
            <StatusChip
              status={member.isActive ? 'published' : 'draft'}
              label={member.isActive ? t('admin.common.active') : t('admin.common.inactive')}
            />
          </Td>
          <Td>{member.order}</Td>
          <Td align="right">
            <div className="inline-flex items-center justify-end gap-1">
              <Link
                to={`/admin/team-members/${member.id}`}
                aria-label={t('admin.common.view')}
                title={t('admin.common.view')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-eye-line text-lg" aria-hidden="true" />
              </Link>
              <Link
                to={`/admin/team-members/${member.id}/edit`}
                aria-label={t('admin.common.edit')}
                title={t('admin.common.edit')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-edit-line text-lg" aria-hidden="true" />
              </Link>
              <button
                type="button"
                onClick={() => handleToggleStatus(member.id)}
                aria-label={member.isActive ? t('admin.team.deactivate') : t('admin.team.activate')}
                title={member.isActive ? t('admin.team.deactivate') : t('admin.team.activate')}
                className={`inline-flex min-h-[44px] min-w-[44px] items-center justify-center transition-colors ${
                  member.isActive ? 'text-gold-ink hover:text-green' : 'text-green hover:text-green-deep'
                }`}
              >
                <i className={member.isActive ? 'ri-pause-circle-line text-lg' : 'ri-play-circle-line text-lg'} aria-hidden="true" />
              </button>
              <button
                type="button"
                onClick={() => handleDelete(member.id)}
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

export default TeamMembersPage;
