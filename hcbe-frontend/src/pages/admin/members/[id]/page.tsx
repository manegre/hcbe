import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { membersApi } from '../../../../lib/api/members';
import { usersApi } from '../../../../lib/api/users';
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
  const [promoting, setPromoting] = useState(false);
  const [promotionMessage, setPromotionMessage] = useState<string | null>(null);
  const [promotionError, setPromotionError] = useState<string | null>(null);

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

  const handlePromote = async () => {
    if (!id || !member || member.isAdmin) return;
    if (!window.confirm(t('admin.members.confirmPromote', { name: `${member.firstName} ${member.lastName}` }))) return;

    try {
      setPromoting(true);
      setPromotionError(null);
      setPromotionMessage(null);
      const response = await usersApi.promoteMember(id);
      if (response.success && response.data) {
        setMember((current) => current ? { ...current, isAdmin: true } : current);
        setPromotionMessage(t('admin.members.promoteSuccess', { email: member.email }));
      } else {
        setPromotionError(response.message || t('admin.members.promoteError'));
      }
    } catch (err) {
      console.error('Error promoting member:', err);
      setPromotionError(t('admin.members.promoteError'));
    } finally {
      setPromoting(false);
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
          {!member.isAdmin && (
            <Button variant="primary" onClick={handlePromote} disabled={promoting}>
              <i className="ri-shield-user-line text-base" aria-hidden="true" />
              {promoting ? t('admin.members.promoting') : t('admin.members.promote')}
            </Button>
          )}
          <Button to={`/admin/members/${member.id}/edit`} variant="secondary">
            {t('admin.common.edit')}
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            {t('admin.common.delete')}
          </Button>
        </>
      }
      main={
        <>
          <div className={`flex flex-wrap items-center justify-between gap-4 rounded-xl border px-4 py-4 sm:px-5 ${member.isAdmin ? 'border-green/25 bg-green/[0.055]' : 'border-gold/35 bg-gold/[0.07]'}`}>
            <div className="flex items-start gap-3">
              <span className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full ${member.isAdmin ? 'bg-green text-gold' : 'bg-gold text-green-deep'}`}>
                <i className={`${member.isAdmin ? 'ri-shield-check-line' : 'ri-user-line'} text-lg`} aria-hidden="true" />
              </span>
              <div>
                <p className="text-[10px] font-bold uppercase tracking-[0.15em] text-ink-variant">{t('admin.members.accessLevel')}</p>
                <p className="mt-1 font-display text-xl font-bold text-green-deep">
                  {member.isAdmin ? t('admin.members.adminAccess') : t('admin.members.memberAccess')}
                </p>
                <p className="mt-1 max-w-2xl text-sm leading-5 text-ink-variant">
                  {member.isAdmin ? t('admin.members.adminAccessHint') : t('admin.members.promoteHint')}
                </p>
              </div>
            </div>
          </div>

          {promotionMessage && (
            <div role="status" className="mt-4 border-l-4 border-green bg-green/[0.06] px-4 py-3 text-sm text-green-deep">
              {promotionMessage}
            </div>
          )}
          {promotionError && (
            <div role="alert" className="mt-4 border-l-4 border-error bg-error/[0.06] px-4 py-3 text-sm text-error-deep">
              {promotionError}
            </div>
          )}

          <DetailList>
            {fields.map((field) => (
              <DetailRow key={field.label} label={field.label} value={field.value || t('admin.common.na')} />
            ))}
          </DetailList>
        </>
      }
    />
  );
};

export default MemberViewPage;
