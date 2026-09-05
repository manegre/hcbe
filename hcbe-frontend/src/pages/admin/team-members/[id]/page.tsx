import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useNavigate } from 'react-router-dom';
import { teamMembersApi } from '../../../../lib/api/team-members';
import type { TeamMemberDto } from '../../../../lib/api/types';
import { buildApiUrl } from '../../../../lib/api/base-url';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState, RichTextContent } from '../../../../components/ui';

const TeamMemberDetailPage: React.FC = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language.startsWith('en') ? 'en-CA' : 'fr-CA';
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [member, setMember] = useState<TeamMemberDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadMember = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const response = await teamMembersApi.getTeamMemberById(id);
        if (response.success && response.data) {
          setMember(response.data);
        } else {
          setError(t('admin.team.errorLoad'));
        }
      } catch (err) {
        console.error('Error loading team member:', err);
        setError(t('admin.team.errorLoad'));
      } finally {
        setLoading(false);
      }
    };

    loadMember();
  }, [id]);

  const handleDelete = async () => {
    if (!id || !window.confirm(t('admin.team.confirmDelete'))) {
      return;
    }

    try {
      const response = await teamMembersApi.deleteTeamMember(id);
      if (response.success) {
        navigate('/admin/team-members');
      }
    } catch (err) {
      console.error('Error deleting team member:', err);
      alert(t('admin.team.errorDelete'));
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error || !member) {
    return (
      <EmptyState
        tone="error"
        title={error || t('admin.team.notFound')}
        action={
          <Button to="/admin/team-members" variant="secondary">
            {t('admin.common.backToList')}
          </Button>
        }
      />
    );
  }

  return (
    <AdminDetailLayout
      title={member.name}
      backPath="/admin/team-members"
      status={{
        status: member.isActive ? 'published' : 'draft',
        label: member.isActive ? t('admin.common.active') : t('admin.common.inactive'),
      }}
      actions={
        <>
          <Button to={`/admin/team-members/${id}/edit`} variant="secondary">
            {t('admin.common.edit')}
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            {t('admin.common.delete')}
          </Button>
        </>
      }
      main={
        <>
          <img
            className="h-32 w-32 border border-line object-cover"
            src={member.photo || buildApiUrl('/api/placeholder/128/128')}
            alt={member.name}
          />

          <DetailList>
            <DetailRow label={t('admin.team.positionFr')} value={member.position} />
            <DetailRow label={t('admin.team.positionEn')} value={member.positionEn || 'N/A'} />
            <DetailRow label={t('admin.common.email')} value={member.email || 'N/A'} />
            <DetailRow label={t('admin.team.regionFr')} value={member.region} />
            <DetailRow label={t('admin.team.regionEn')} value={member.regionEn || 'N/A'} />
            <DetailRow label={t('admin.common.zone')} value={member.zone} />
            <DetailRow label={t('admin.team.zoneEn')} value={member.zoneEn || 'N/A'} />
            <DetailRow label={t('admin.common.order')} value={member.order} />
            <DetailRow label={t('admin.team.createdAt')} value={new Date(member.createdAt).toLocaleDateString(locale)} />
            <DetailRow label={t('admin.team.updatedAt')} value={new Date(member.updatedAt).toLocaleDateString(locale)} />
          </DetailList>

          {member.bio && (
            <div>
              <h2 className="font-display text-headline-sm text-green">{t('admin.team.biographyFr')}</h2>
              <RichTextContent value={member.bio} className="mt-3 text-body-md text-ink-variant" />
            </div>
          )}
          {member.bioEn && <div><h2 className="font-display text-headline-sm text-green">{t('admin.team.biographyEn')}</h2><RichTextContent value={member.bioEn} className="mt-3 text-body-md text-ink-variant" /></div>}
        </>
      }
    />
  );
};

export default TeamMemberDetailPage;
