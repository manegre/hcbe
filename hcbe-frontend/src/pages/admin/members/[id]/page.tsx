import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { membersApi } from '../../../../lib/api/members';
import type { MemberDto } from '../../../../lib/api/types';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState } from '../../../../components/ui';

const MemberViewPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [member, setMember] = useState<MemberDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadMember = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const response = await membersApi.getMemberById(id);
        if (response.success && response.data) {
          setMember(response.data);
        } else {
          setError(t('admin.members.errorLoad'));
        }
      } catch (err) {
        console.error('Error loading member:', err);
        setError(t('admin.members.errorLoad'));
      } finally {
        setLoading(false);
      }
    };

    loadMember();
  }, [id, t]);

  const handleDelete = async () => {
    if (!id || !member) return;
    if (!window.confirm(t('admin.common.confirmDelete', { name: `${member.firstName} ${member.lastName}` }))) {
      return;
    }

    try {
      const response = await membersApi.deleteMember(id);
      if (response.success) {
        navigate('/admin/members');
      }
    } catch (err) {
      console.error('Error deleting member:', err);
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
        title={error || t('admin.members.errorLoad')}
        action={
          <Button to="/admin/members" variant="secondary">
            {t('admin.common.back')}
          </Button>
        }
      />
    );
  }

  const fields = [
    { label: t('admin.common.email'), value: member.email },
    { label: t('admin.members.phone'), value: member.phone },
    { label: t('admin.members.city'), value: member.city },
    { label: t('admin.members.province'), value: member.province },
    { label: t('admin.members.profession'), value: member.profession },
    { label: t('admin.members.expertise'), value: member.expertise },
    { label: t('admin.common.zone'), value: member.zone },
    { label: t('admin.members.interests'), value: member.interests },
    { label: t('admin.members.availability'), value: member.availability },
  ];

  return (
    <AdminDetailLayout
      title={`${member.firstName} ${member.lastName}`}
      subtitle={t('admin.members.memberSince', { date: new Date(member.createdAt).toLocaleDateString() })}
      backPath="/admin/members"
      actions={
        <>
          <Button to={`/admin/members/${member.id}/edit`} variant="secondary">
            {t('admin.common.edit')}
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            {t('admin.common.delete')}
          </Button>
        </>
      }
      main={
        <DetailList>
          {fields.map((field) => (
            <DetailRow key={field.label} label={field.label} value={field.value || t('admin.common.na')} />
          ))}
        </DetailList>
      }
    />
  );
};

export default MemberViewPage;
