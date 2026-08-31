import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { publicSubmissionsApi } from '../../../lib/api/public-submissions';
import type { PublicSubmissionDto, PublicSubmissionType } from '../../../lib/api/types';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { Field, StatusChip, Td, inputClasses } from '../../../components/ui';

type SubmissionStatus = PublicSubmissionDto['status'];

const submissionTypes: PublicSubmissionType[] = [
  'contact',
  'volunteer',
  'event-registration',
  'grant-application',
  'consultation-response',
  'project-contribution',
];

const statuses: SubmissionStatus[] = ['Pending', 'InReview', 'Resolved', 'Rejected'];

const statusTone = (status: SubmissionStatus): 'pending' | 'draft' | 'approved' | 'rejected' => {
  if (status === 'InReview') return 'draft';
  if (status === 'Resolved') return 'approved';
  if (status === 'Rejected') return 'rejected';
  return 'pending';
};

const AdminSubmissionsPage = () => {
  const { t } = useTranslation();
  const [items, setItems] = useState<PublicSubmissionDto[]>([]);
  const [typeFilter, setTypeFilter] = useState<PublicSubmissionType | 'all'>('all');
  const [statusFilter, setStatusFilter] = useState<SubmissionStatus | 'all'>('Pending');
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  const loadItems = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await publicSubmissionsApi.search({
        page,
        search,
        type: typeFilter === 'all' ? undefined : typeFilter,
        status: statusFilter === 'all' ? undefined : statusFilter,
      });
      if (!response.success || !response.data) throw new Error(response.message);
      setItems(response.data.items);
      setTotalItems(response.data.totalItems);
      setTotalPages(response.data.totalPages);
    } catch (loadError) {
      console.error('Unable to load public submissions:', loadError);
      setError(t('admin.submissions.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadItems();
  }, [typeFilter, statusFilter, page, search]);

  const pendingCount = useMemo(() => items.filter((item) => item.status === 'Pending').length, [items]);

  const updateStatus = async (item: PublicSubmissionDto, status: SubmissionStatus) => {
    setBusyId(item.id);
    setError(null);
    try {
      const response = await publicSubmissionsApi.updateStatus(item.id, status);
      if (!response.success || !response.data) throw new Error(response.message);
      setItems((current) => current.map((candidate) => candidate.id === item.id ? response.data! : candidate));
    } catch (updateError) {
      console.error('Unable to update submission:', updateError);
      setError(t('admin.submissions.errorUpdate'));
    } finally {
      setBusyId(null);
    }
  };

  const deleteItem = async (item: PublicSubmissionDto) => {
    if (!window.confirm(t('admin.common.confirmDeleteGeneric'))) return;
    setBusyId(item.id);
    try {
      const response = await publicSubmissionsApi.delete(item.id);
      if (!response.success) throw new Error(response.message);
      setItems((current) => current.filter((candidate) => candidate.id !== item.id));
    } catch (deleteError) {
      console.error('Unable to delete submission:', deleteError);
      setError(t('admin.common.errorDelete'));
    } finally {
      setBusyId(null);
    }
  };

  const toolbar = (
    <>
      <Field label={t('admin.list.search')} htmlFor="submission-search">
        <input id="submission-search" className={inputClasses} value={search} placeholder={t('admin.list.searchPlaceholder')} onChange={(event) => { setSearch(event.target.value); setPage(1); }} />
      </Field>
      <Field label={t('admin.common.type')} htmlFor="submission-type">
        <select id="submission-type" className={inputClasses} value={typeFilter} onChange={(event) => { setTypeFilter(event.target.value as PublicSubmissionType | 'all'); setPage(1); }}>
          <option value="all">{t('admin.submissions.allTypes')}</option>
          {submissionTypes.map((type) => <option key={type} value={type}>{t(`admin.submissions.type.${type}`)}</option>)}
        </select>
      </Field>
      <Field label={t('admin.common.status')} htmlFor="submission-status">
        <select id="submission-status" className={inputClasses} value={statusFilter} onChange={(event) => { setStatusFilter(event.target.value as SubmissionStatus | 'all'); setPage(1); }}>
          <option value="all">{t('admin.submissions.allStatuses')}</option>
          {statuses.map((status) => <option key={status} value={status}>{t(`admin.submissions.status.${status}`)}</option>)}
        </select>
      </Field>
    </>
  );

  if (loading) {
    return <div className="flex items-center justify-center py-24"><div className="h-8 w-8 animate-spin border-2 border-line border-t-green" /></div>;
  }

  return (
    <AdminListPage
      title={t('admin.submissions.title')}
      count={totalItems}
      toolbar={toolbar}
      columns={[
        { key: 'sender', label: t('admin.submissions.sender') },
        { key: 'type', label: t('admin.common.type') },
        { key: 'subject', label: t('admin.common.title') },
        { key: 'date', label: t('admin.common.date') },
        { key: 'status', label: t('admin.common.status') },
        { key: 'actions', label: t('admin.common.actions'), align: 'right' },
      ]}
      isEmpty={items.length === 0}
      emptyTitle={t('admin.submissions.empty')}
      emptyDescription={pendingCount === 0 && statusFilter === 'Pending' ? t('admin.submissions.emptyPending') : undefined}
      error={error ?? undefined}
      onRetry={loadItems}
      pagination={{ page, totalPages, totalItems, onPageChange: setPage }}
    >
      {items.map((item) => (
        <FragmentRow
          key={item.id}
          item={item}
          expanded={expandedId === item.id}
          busy={busyId === item.id}
          onToggle={() => setExpandedId((current) => current === item.id ? null : item.id)}
          onUpdateStatus={updateStatus}
          onDelete={deleteItem}
          t={t}
        />
      ))}
    </AdminListPage>
  );
};

interface FragmentRowProps {
  item: PublicSubmissionDto;
  expanded: boolean;
  busy: boolean;
  onToggle: () => void;
  onUpdateStatus: (item: PublicSubmissionDto, status: SubmissionStatus) => void;
  onDelete: (item: PublicSubmissionDto) => void;
  t: ReturnType<typeof useTranslation>['t'];
}

const FragmentRow = ({ item, expanded, busy, onToggle, onUpdateStatus, onDelete, t }: FragmentRowProps) => (
  <>
    <tr className="transition-colors hover:bg-surface-container">
      <Td><div className="font-semibold text-ink">{item.firstName} {item.lastName}</div><a className="text-sm text-green hover:underline" href={`mailto:${item.email}`}>{item.email}</a></Td>
      <Td>{t(`admin.submissions.type.${item.type}`)}</Td>
      <Td className="max-w-[280px]"><span className="line-clamp-2">{item.subject || item.details}</span></Td>
      <Td>{new Date(item.createdAt).toLocaleDateString()}</Td>
      <Td><StatusChip status={statusTone(item.status)} label={t(`admin.submissions.status.${item.status}`)} /></Td>
      <Td align="right">
        <div className="inline-flex items-center gap-1">
          <button type="button" onClick={onToggle} className="inline-flex h-10 w-10 items-center justify-center rounded-lg text-green hover:bg-green/10" aria-label={t('admin.common.view')} title={t('admin.common.view')}>
            <i className={expanded ? 'ri-arrow-up-s-line text-lg' : 'ri-eye-line text-lg'} aria-hidden="true" />
          </button>
          <select
            aria-label={t('admin.submissions.changeStatus')}
            className="h-10 rounded-lg border border-line bg-surface px-2 text-sm text-ink"
            value={item.status}
            disabled={busy}
            onChange={(event) => onUpdateStatus(item, event.target.value as SubmissionStatus)}
          >
            {statuses.map((status) => <option key={status} value={status}>{t(`admin.submissions.status.${status}`)}</option>)}
          </select>
          <button type="button" disabled={busy} onClick={() => onDelete(item)} className="inline-flex h-10 w-10 items-center justify-center rounded-lg text-error hover:bg-error/10 disabled:opacity-50" aria-label={t('admin.common.delete')} title={t('admin.common.delete')}>
            <i className="ri-delete-bin-line text-lg" aria-hidden="true" />
          </button>
        </div>
      </Td>
    </tr>
    {expanded && (
      <tr className="bg-surface-container/60">
        <td colSpan={6} className="px-5 py-5">
          <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_280px]">
            <div><p className="text-[10px] font-bold uppercase tracking-[0.16em] text-ink-variant">{t('admin.submissions.message')}</p><p className="mt-2 whitespace-pre-wrap leading-7 text-ink">{item.details}</p></div>
            <dl className="space-y-2 rounded-xl border border-line bg-surface p-4 text-sm">
              {item.phone && <div><dt className="text-ink-variant">{t('admin.submissions.phone')}</dt><dd><a className="text-green hover:underline" href={`tel:${item.phone}`}>{item.phone}</a></dd></div>}
              {item.city && <div><dt className="text-ink-variant">{t('admin.common.location')}</dt><dd>{item.city}</dd></div>}
              {item.metadataJson && <div><dt className="text-ink-variant">{t('admin.submissions.context')}</dt><dd className="break-words">{item.metadataJson}</dd></div>}
            </dl>
          </div>
        </td>
      </tr>
    )}
  </>
);

export default AdminSubmissionsPage;
