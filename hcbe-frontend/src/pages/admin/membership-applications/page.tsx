import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { membershipApplicationsApi } from '../../../lib/api/membership-applications';
import type { MembershipApplicationDto, MembershipApplicationStatus } from '../../../lib/api/types';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { Field, StatusChip, Td, inputClasses } from '../../../components/ui';

const statusChipStatus = (status: MembershipApplicationStatus): 'pending' | 'approved' | 'rejected' => {
  if (status === 'Approved') return 'approved';
  if (status === 'Rejected') return 'rejected';
  return 'pending';
};

const MembershipApplicationsPage: React.FC = () => {
  const { t } = useTranslation();
  const [applications, setApplications] = useState<MembershipApplicationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<MembershipApplicationStatus | 'all'>('Pending');
  const [actionId, setActionId] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  const loadApplications = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await membershipApplicationsApi.search({
        page,
        search,
        status: statusFilter === 'all' ? undefined : statusFilter,
      });
      if (response.success && response.data) {
        setApplications(response.data.items);
        setTotalItems(response.data.totalItems);
        setTotalPages(response.data.totalPages);
      } else {
        setError(t('admin.applications.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading applications:', err);
      setError(t('admin.applications.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadApplications();
  }, [statusFilter, page, search]);

  const handleApprove = async (id: string) => {
    if (!window.confirm(t('admin.applications.confirmApprove'))) return;

    try {
      setActionId(id);
      const response = await membershipApplicationsApi.approve(id);
      if (response.success) {
        loadApplications();
      }
    } catch (err) {
      console.error('Error approving application:', err);
    } finally {
      setActionId(null);
    }
  };

  const handleReject = async (id: string) => {
    if (!window.confirm(t('admin.applications.confirmReject'))) return;

    try {
      setActionId(id);
      const response = await membershipApplicationsApi.reject(id);
      if (response.success) {
        loadApplications();
      }
    } catch (err) {
      console.error('Error rejecting application:', err);
    } finally {
      setActionId(null);
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm(t('admin.common.confirmDeleteGeneric'))) return;

    try {
      const response = await membershipApplicationsApi.delete(id);
      if (response.success) {
        loadApplications();
      }
    } catch (err) {
      console.error('Error deleting application:', err);
    }
  };

  const toolbar = (
    <>
      <Field label={t('admin.list.search')} htmlFor="application-search">
        <input id="application-search" className={inputClasses} value={search} placeholder={t('admin.list.searchPlaceholder')} onChange={(event) => { setSearch(event.target.value); setPage(1); }} />
      </Field>
      <Field label={t('admin.common.filterBy')} htmlFor="application-status">
        <select
          id="application-status"
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value as MembershipApplicationStatus | 'all'); setPage(1); }}
          className={inputClasses}
        >
          <option value="Pending">{t('admin.applications.filterPending')}</option>
          <option value="Approved">{t('admin.applications.filterApproved')}</option>
          <option value="Rejected">{t('admin.applications.filterRejected')}</option>
          <option value="all">{t('admin.applications.filterAll')}</option>
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
      title={t('admin.applications.title')}
      count={error ? undefined : totalItems}
      toolbar={toolbar}
      columns={[
        { key: 'applicant', label: t('admin.applications.colApplicant') },
        { key: 'location', label: t('admin.common.location') },
        { key: 'status', label: t('admin.common.status') },
        { key: 'date', label: t('admin.common.date') },
        { key: 'actions', label: t('admin.common.actions'), align: 'right' },
      ]}
      isEmpty={applications.length === 0}
      emptyTitle={t('admin.applications.emptyTitle')}
      error={error ?? undefined}
      onRetry={loadApplications}
      pagination={{ page, totalPages, totalItems, onPageChange: setPage }}
    >
      {applications.map((application) => (
        <tr key={application.id} className="transition-colors hover:bg-surface-container">
          <Td className="text-ink">
            <div className="font-medium">
              {application.firstName} {application.lastName}
            </div>
            <div className="text-ink-variant">{application.email}</div>
          </Td>
          <Td>
            {[application.city, application.province].filter(Boolean).join(', ') || t('admin.common.na')}
          </Td>
          <Td>
            <StatusChip
              status={statusChipStatus(application.status)}
              label={t(`admin.applications.status.${application.status.toLowerCase()}`)}
            />
          </Td>
          <Td>{new Date(application.createdAt).toLocaleDateString()}</Td>
          <Td align="right">
            <div className="inline-flex items-center justify-end gap-1">
              <Link
                to={`/admin/membership-applications/${application.id}`}
                aria-label={t('admin.common.view')}
                title={t('admin.common.view')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-eye-line text-lg" aria-hidden="true" />
              </Link>
              {application.status === 'Pending' && (
                <>
                  <button
                    type="button"
                    onClick={() => handleApprove(application.id)}
                    disabled={actionId === application.id}
                    aria-label={t('admin.applications.approve')}
                    title={t('admin.applications.approve')}
                    className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep disabled:opacity-50"
                  >
                    <i className="ri-check-line text-lg" aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    onClick={() => handleReject(application.id)}
                    disabled={actionId === application.id}
                    aria-label={t('admin.applications.reject')}
                    title={t('admin.applications.reject')}
                    className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center rounded-control text-error transition-colors hover:text-error-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-error disabled:opacity-50"
                  >
                    <i className="ri-close-line text-lg" aria-hidden="true" />
                  </button>
                </>
              )}
              {application.status === 'Approved' && application.memberId && (
                <Link
                  to={`/admin/members/${application.memberId}`}
                  aria-label={t('admin.applications.viewMember')}
                  title={t('admin.applications.viewMember')}
                  className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
                >
                  <i className="ri-user-line text-lg" aria-hidden="true" />
                </Link>
              )}
              <button
                type="button"
                onClick={() => handleDelete(application.id)}
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

export default MembershipApplicationsPage;
