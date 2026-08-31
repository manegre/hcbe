import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { membersApi } from '../../../lib/api/members';
import type { MemberDto } from '../../../lib/api/types';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { Field, Td, inputClasses } from '../../../components/ui';

const MembersPage: React.FC = () => {
  const [members, setMembers] = useState<MemberDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [sort, setSort] = useState('newest');
  const [page, setPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const { t } = useTranslation();

  const loadMembers = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await membersApi.searchMembers({ page, search, sort });
      if (response.success && response.data) {
        setMembers(response.data.items);
        setTotalItems(response.data.totalItems);
        setTotalPages(response.data.totalPages);
      } else {
        setError(t('admin.members.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading members:', err);
      setError(t('admin.members.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadMembers();
  }, [page, search, sort]);

  const handleDelete = async (id: string, name: string) => {
    if (!window.confirm(t('admin.common.confirmDelete', { name }))) {
      return;
    }

    try {
      const response = await membersApi.deleteMember(id);
      if (response.success) {
        loadMembers();
      }
    } catch (err) {
      console.error('Error deleting member:', err);
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
      title={t('admin.members.title')}
      count={error ? undefined : totalItems}
      createLabel={t('admin.members.create')}
      createPath="/admin/members/create"
      toolbar={(
        <>
          <Field label={t('admin.list.search')} htmlFor="member-search">
            <input id="member-search" className={inputClasses} value={search} placeholder={t('admin.list.searchPlaceholder')} onChange={(event) => { setSearch(event.target.value); setPage(1); }} />
          </Field>
          <Field label={t('admin.common.sort')} htmlFor="member-sort">
            <select id="member-sort" className={inputClasses} value={sort} onChange={(event) => { setSort(event.target.value); setPage(1); }}>
              <option value="newest">{t('admin.common.newest')}</option>
              <option value="oldest">{t('admin.common.oldest')}</option>
              <option value="name">{t('admin.common.name')}</option>
            </select>
          </Field>
        </>
      )}
      columns={[
        { key: 'member', label: t('admin.members.colMember') },
        { key: 'location', label: t('admin.common.location') },
        { key: 'profession', label: t('admin.members.colProfession') },
        { key: 'date', label: t('admin.common.date') },
        { key: 'actions', label: t('admin.common.actions'), align: 'right' },
      ]}
      isEmpty={members.length === 0}
      emptyTitle={t('admin.members.emptyTitle')}
      error={error ?? undefined}
      onRetry={loadMembers}
      pagination={{ page, totalPages, totalItems, onPageChange: setPage }}
    >
      {members.map((member) => (
        <tr key={member.id} className="transition-colors hover:bg-surface-container">
          <Td className="text-ink">
            <div className="font-medium">
              {member.firstName} {member.lastName}
            </div>
            <div className="text-ink-variant">{member.email}</div>
          </Td>
          <Td>{[member.city, member.province].filter(Boolean).join(', ') || t('admin.common.na')}</Td>
          <Td>{member.profession || t('admin.common.na')}</Td>
          <Td>{new Date(member.createdAt).toLocaleDateString()}</Td>
          <Td align="right">
            <div className="inline-flex items-center justify-end gap-1">
              <Link
                to={`/admin/members/${member.id}`}
                aria-label={t('admin.common.view')}
                title={t('admin.common.view')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-eye-line text-lg" aria-hidden="true" />
              </Link>
              <Link
                to={`/admin/members/${member.id}/edit`}
                aria-label={t('admin.common.edit')}
                title={t('admin.common.edit')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-edit-line text-lg" aria-hidden="true" />
              </Link>
              <button
                type="button"
                onClick={() => handleDelete(member.id, `${member.firstName} ${member.lastName}`)}
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

export default MembersPage;
