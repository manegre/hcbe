import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AdminDetailLayout, DetailList, DetailRow } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState, Field, inputClasses } from '../../../../components/ui';
import { membershipApplicationsApi } from '../../../../lib/api/membership-applications';
import type { MembershipApplicationDto } from '../../../../lib/api/types';

const statusChipStatus = (status: string): 'pending' | 'approved' | 'rejected' => {
  if (status === 'Approved') return 'approved';
  if (status === 'Rejected') return 'rejected';
  return 'pending';
};

const MembershipApplicationViewPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [application, setApplication] = useState<MembershipApplicationDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showApproveConfirm, setShowApproveConfirm] = useState(false);
  const [showRejectForm, setShowRejectForm] = useState(false);
  const [motif, setMotif] = useState('');

  const loadApplication = async () => {
    if (!id) return;

    try {
      setLoading(true);
      const response = await membershipApplicationsApi.getById(id);
      if (response.success && response.data) {
        setApplication(response.data);
      } else {
        setError(t('admin.applications.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading application:', err);
      setError(t('admin.applications.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadApplication();
  }, [id]);

  const handleApprove = async () => {
    if (!id) return;

    try {
      setActionLoading(true);
      const response = await membershipApplicationsApi.approve(id);
      if (response.success && response.data) {
        navigate(`/admin/members/${response.data.id}`);
      } else {
        setShowApproveConfirm(false);
        await loadApplication();
      }
    } catch (err) {
      console.error('Error approving application:', err);
    } finally {
      setActionLoading(false);
    }
  };

  const handleReject = async () => {
    if (!id) return;

    try {
      setActionLoading(true);
      const response = await membershipApplicationsApi.reject(id, motif.trim() || undefined);
      if (response.success) {
        setShowRejectForm(false);
        setMotif('');
        await loadApplication();
      }
    } catch (err) {
      console.error('Error rejecting application:', err);
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error || !application) {
    return (
      <EmptyState
        tone="error"
        title={t('admin.applications.errorLoadTitle')}
        description={error ?? t('admin.applications.errorLoad')}
        action={
          <Button to="/admin/membership-applications" variant="secondary">
            {t('admin.common.backToList')}
          </Button>
        }
      />
    );
  }

  const fullName = `${application.firstName} ${application.lastName}`;

  const fields = [
    { label: t('admin.common.email'), value: application.email },
    { label: t('admin.members.phone'), value: application.phone },
    { label: t('admin.members.city'), value: application.city },
    { label: t('admin.members.province'), value: application.province },
    { label: t('admin.members.profession'), value: application.profession },
    { label: t('admin.members.expertise'), value: application.expertise },
  ];

  return (
    <AdminDetailLayout
      title={fullName}
      subtitle={t('admin.applications.detailSubtitle')}
      backPath="/admin/membership-applications"
      status={{
        status: statusChipStatus(application.status),
        label: t(`admin.applications.status.${application.status.toLowerCase()}`),
      }}
      secondaryActions={
        application.status === 'Approved' && application.memberId ? (
          <Button to={`/admin/members/${application.memberId}`} variant="secondary">
            {t('admin.applications.viewMember')}
          </Button>
        ) : undefined
      }
      main={
        <>
          <DetailList>
            {fields.map((field) => (
              <DetailRow key={field.label} label={field.label} value={field.value || t('admin.common.na')} />
            ))}
          </DetailList>

          {application.motivation && (
            <div>
              <h2 className="font-display text-headline-sm text-green">{t('admin.applications.motivation')}</h2>
              <p className="mt-3 whitespace-pre-wrap text-body-md text-ink-variant">{application.motivation}</p>
            </div>
          )}
        </>
      }
      aside={
        <div className="border border-line bg-surface p-6">
          <h2 className="font-display text-headline-sm text-green">{t('admin.applications.decisionTitle')}</h2>

          {application.status === 'Pending' && !showApproveConfirm && !showRejectForm && (
            <div className="mt-4 flex flex-col gap-3">
              <Button variant="primary" onClick={() => setShowApproveConfirm(true)} disabled={actionLoading}>
                {t('admin.applications.approve')}
              </Button>
              <Button variant="destructive" onClick={() => setShowRejectForm(true)} disabled={actionLoading}>
                {t('admin.applications.reject')}
              </Button>
            </div>
          )}

          {showApproveConfirm && (
            <div className="mt-4 border border-line bg-surface p-8">
              <p className="text-body-md text-ink">{t('admin.applications.approveDialogTitle', { name: fullName })}</p>
              <p className="mt-2 text-body-md text-ink-variant">
                {t('admin.applications.approveDialogBody', { name: fullName })}
              </p>
              <div className="mt-6 flex flex-wrap gap-3">
                <Button
                  variant="secondary"
                  onClick={() => setShowApproveConfirm(false)}
                  disabled={actionLoading}
                >
                  {t('admin.common.cancel')}
                </Button>
                <Button variant="primary" onClick={handleApprove} disabled={actionLoading}>
                  {t('admin.applications.approve')}
                </Button>
              </div>
            </div>
          )}

          {showRejectForm && (
            <div className="mt-4 flex flex-col gap-4">
              <Field label={t('admin.applications.motif')} htmlFor="reject-motif">
                <textarea
                  id="reject-motif"
                  value={motif}
                  onChange={(e) => setMotif(e.target.value)}
                  rows={4}
                  className={inputClasses}
                />
              </Field>
              <div className="flex flex-wrap gap-3">
                <Button
                  variant="secondary"
                  onClick={() => {
                    setShowRejectForm(false);
                    setMotif('');
                  }}
                  disabled={actionLoading}
                >
                  {t('admin.common.cancel')}
                </Button>
                <Button variant="destructive" onClick={handleReject} disabled={actionLoading}>
                  {t('admin.applications.confirmRejectAction')}
                </Button>
              </div>
            </div>
          )}

          <div className="mt-6 divide-y divide-line border-y border-line">
            <div className="py-3">
              <p className="text-label-md uppercase text-ink-variant">{t('admin.applications.activityReceived')}</p>
              <p className="mt-1 text-body-md text-ink">{new Date(application.createdAt).toLocaleString()}</p>
            </div>
            {application.reviewedAt && (
              <div className="py-3">
                <p className="text-label-md uppercase text-ink-variant">{t('admin.applications.activityDecided')}</p>
                <p className="mt-1 text-body-md text-ink">{new Date(application.reviewedAt).toLocaleString()}</p>
              </div>
            )}
          </div>
        </div>
      }
    />
  );
};

export default MembershipApplicationViewPage;
