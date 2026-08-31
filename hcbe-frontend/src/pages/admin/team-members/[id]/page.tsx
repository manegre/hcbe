import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { teamMembersApi } from '../../../../lib/api/team-members';
import type { TeamMemberDto } from '../../../../lib/api/types';
import { buildApiUrl } from '../../../../lib/api/base-url';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState } from '../../../../components/ui';

const TeamMemberDetailPage: React.FC = () => {
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
          setError('Failed to load team member');
        }
      } catch (err) {
        console.error('Error loading team member:', err);
        setError('Error loading team member');
      } finally {
        setLoading(false);
      }
    };

    loadMember();
  }, [id]);

  const handleDelete = async () => {
    if (!id || !window.confirm('Are you sure you want to delete this team member?')) {
      return;
    }

    try {
      const response = await teamMembersApi.deleteTeamMember(id);
      if (response.success) {
        navigate('/admin/team-members');
      }
    } catch (err) {
      console.error('Error deleting team member:', err);
      alert('Failed to delete team member');
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
        title={error || 'Team member not found'}
        action={
          <Button to="/admin/team-members" variant="secondary">
            Back to list
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
        label: member.isActive ? 'Active' : 'Inactive',
      }}
      actions={
        <>
          <Button to={`/admin/team-members/${id}/edit`} variant="secondary">
            Edit
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            Delete
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
            <DetailRow label="Position" value={member.position} />
            <DetailRow label="Position (English)" value={member.positionEn || 'N/A'} />
            <DetailRow label="Email" value={member.email || 'N/A'} />
            <DetailRow label="Region" value={member.region} />
            <DetailRow label="Region (English)" value={member.regionEn || 'N/A'} />
            <DetailRow label="Zone" value={member.zone} />
            <DetailRow label="Zone (English)" value={member.zoneEn || 'N/A'} />
            <DetailRow label="Display Order" value={member.order} />
            <DetailRow label="Created" value={new Date(member.createdAt).toLocaleDateString()} />
            <DetailRow label="Updated" value={new Date(member.updatedAt).toLocaleDateString()} />
          </DetailList>

          {member.bio && (
            <div>
              <h2 className="font-display text-headline-sm text-green">Biography</h2>
              <p className="mt-3 text-body-md text-ink-variant">{member.bio}</p>
            </div>
          )}
          {member.bioEn && <div><h2 className="font-display text-headline-sm text-green">Biography (English)</h2><p className="mt-3 text-body-md text-ink-variant">{member.bioEn}</p></div>}
        </>
      }
    />
  );
};

export default TeamMemberDetailPage;
